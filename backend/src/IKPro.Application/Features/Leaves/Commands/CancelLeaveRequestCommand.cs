using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Leaves.Commands;

/// <summary>Kendi bekleyen talebini iptal eder.</summary>
public sealed record CancelLeaveRequestCommand(int Id) : IRequest;

public sealed class CancelLeaveRequestCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CancelLeaveRequestCommand>
{
    public async Task Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employeeId = LeaveGuards.RequireEmployeeId(currentUser);

        var leaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("İzin talebi", request.Id);

        if (leaveRequest.EmployeeId != employeeId)
        {
            throw new ForbiddenAccessException("Yalnız kendi talebinizi iptal edebilirsiniz.");
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new ConflictException("Yalnız bekleyen talepler iptal edilebilir.");
        }

        leaveRequest.Status = LeaveStatus.Cancelled;
        await context.SaveChangesAsync(cancellationToken);
    }
}
