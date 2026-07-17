using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Common.Notifications;
using IKPro.Domain.Entities.Leaves;
using IKPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Leaves.Commands;

/// <summary>
/// İzin talebi oluşturur. Gün sayısı SQL iş-günü fonksiyonuyla hesaplanır;
/// çakışan talep ve yetersiz bakiye 409 döner. Onay gerektirmeyen tipler
/// (ör. Raporlu) doğrudan onaylı açılır.
/// </summary>
public sealed record CreateLeaveRequestCommand(
    int LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Description = null,
    int? SubstituteEmployeeId = null) : IRequest<LeaveRequestDto>;

public sealed class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.LeaveTypeId).GreaterThan(0);
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Bitiş tarihi başlangıçtan önce olamaz.");
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class CreateLeaveRequestCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IWorkingDayCalculator workingDayCalculator,
    INotificationTrigger notificationTrigger)
    : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(
        CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employeeId = LeaveGuards.RequireEmployeeId(currentUser);

        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(t => t.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException("İzin tipi", request.LeaveTypeId);

        if (request.SubstituteEmployeeId is not null &&
            !await context.Employees.AnyAsync(e => e.Id == request.SubstituteEmployeeId, cancellationToken))
        {
            throw new NotFoundException("Vekil personel", request.SubstituteEmployeeId);
        }

        var days = await workingDayCalculator.GetWorkingDaysAsync(
            request.StartDate, request.EndDate, cancellationToken);
        if (days == 0)
        {
            throw new ConflictException("Seçilen aralıkta iş günü bulunmuyor (hafta sonu/resmi tatil).");
        }

        var hasOverlap = await context.LeaveRequests.AnyAsync(r =>
            r.EmployeeId == employeeId &&
            (r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.Approved) &&
            r.StartDate <= request.EndDate &&
            r.EndDate >= request.StartDate, cancellationToken);
        if (hasOverlap)
        {
            throw new ConflictException("Bu tarih aralığıyla çakışan bekleyen/onaylı bir talebiniz var.");
        }

        if (leaveType.DeductsFromAnnualBalance)
        {
            // Bekleyen düşümlü talepleri de hesaba katar (view yalnız onaylıyı sayar).
            await LeaveGuards.EnsureBalanceAsync(
                context, employeeId, request.StartDate.Year, days, excludeRequestId: null, cancellationToken);
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveType.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Days = days,
            Description = request.Description,
            SubstituteEmployeeId = request.SubstituteEmployeeId,
            Status = leaveType.RequiresApproval ? LeaveStatus.Pending : LeaveStatus.Approved,
        };

        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync(cancellationToken);

        // Faz 11: "İzin Talepleri" bildirimi (ayar toggle'ına uyar).
        await notificationTrigger.LeaveRequestCreatedAsync(leaveRequest, cancellationToken);

        return await context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Id == leaveRequest.Id)
            .Select(Queries.LeaveQueryProjections.Projection)
            .SingleAsync(cancellationToken);
    }
}
