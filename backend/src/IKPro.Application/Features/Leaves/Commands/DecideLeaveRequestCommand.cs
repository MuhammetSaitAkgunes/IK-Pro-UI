using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Features.Employees;
using IKPro.Domain.Constants;
using IKPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Leaves.Commands;

/// <summary>
/// Onay kuyruğu kararı: approve/reject. Manager yalnız kendi ekibinin taleplerini
/// karara bağlayabilir; kimse (hr-admin hariç) kendi talebini onaylayamaz.
/// </summary>
public sealed record DecideLeaveRequestCommand(int Id, bool Approve, string? DecisionNote = null)
    : IRequest<LeaveRequestDto>;

public sealed class DecideLeaveRequestCommandValidator : AbstractValidator<DecideLeaveRequestCommand>
{
    public DecideLeaveRequestCommandValidator()
    {
        RuleFor(x => x.DecisionNote).MaximumLength(1000);
    }
}

public sealed class DecideLeaveRequestCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DecideLeaveRequestCommand, LeaveRequestDto>
{
    public async Task<LeaveRequestDto> Handle(
        DecideLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("İzin talebi", request.Id);

        var inScope = await context.Employees
            .ScopeFor(currentUser)
            .AnyAsync(e => e.Id == leaveRequest.EmployeeId, cancellationToken);
        if (!inScope)
        {
            throw new ForbiddenAccessException("Bu talep sizin onay kapsamınızda değil.");
        }

        var isHrAdmin = currentUser.Roles.Contains(Roles.HrAdmin);
        if (!isHrAdmin && leaveRequest.EmployeeId == currentUser.EmployeeId)
        {
            throw new ForbiddenAccessException("Kendi izin talebinizi karara bağlayamazsınız.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new ConflictException("Yalnız bekleyen talepler karara bağlanabilir.");
        }

        leaveRequest.Status = request.Approve ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leaveRequest.DecisionByUserId = currentUser.UserId;
        leaveRequest.DecisionAtUtc = DateTime.UtcNow;
        leaveRequest.DecisionNote = request.DecisionNote;

        await context.SaveChangesAsync(cancellationToken);

        return await context.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Id == leaveRequest.Id)
            .Select(Queries.LeaveQueryProjections.Projection)
            .SingleAsync(cancellationToken);
    }
}
