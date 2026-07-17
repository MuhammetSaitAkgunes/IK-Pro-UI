using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Leaves;

public sealed record LeaveTypeDto(
    int Id, string Name, string? Code, bool DeductsFromAnnualBalance, bool RequiresApproval);

/// <summary>İzin talebi — leaves.js geçmiş tablosu + onay kuyruğu şekli (status küçük harf).</summary>
public sealed record LeaveRequestDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    int LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Days,
    string Status,
    string? Description,
    int? SubstituteEmployeeId,
    string? DecisionNote,
    DateTime? DecisionAtUtc,
    DateTime CreatedAtUtc);

/// <summary>Bakiye kartı — leaves.js: hak ediş / kullanılan / kalan.</summary>
public sealed record LeaveBalanceDto(
    int Year, int EntitledDays, int CarriedOverDays, int UsedDays, int RemainingDays);

/// <summary>Takım yokluk widget'ı satırı.</summary>
public sealed record TeamLeaveDto(
    int EmployeeId,
    string EmployeeName,
    string Initials,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate);

public static class LeaveMappings
{
    /// <summary>Frontend durum değerleri: approved | pending | rejected | cancelled.</summary>
    public static string ToDto(this LeaveStatus status) => status switch
    {
        LeaveStatus.Pending => "pending",
        LeaveStatus.Approved => "approved",
        LeaveStatus.Rejected => "rejected",
        LeaveStatus.Cancelled => "cancelled",
        _ => status.ToString().ToLowerInvariant(),
    };
}

public static class LeaveGuards
{
    /// <summary>İzin uçları personel bağı gerektirir (hr-admin demo hesabı gibi bağsız kullanıcılar hariç tutulur).</summary>
    public static int RequireEmployeeId(ICurrentUser currentUser)
        => currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Hesabınıza bağlı personel kaydı yok; izin işlemleri yapılamaz.");

    /// <summary>
    /// Yıllık izin bakiyesi kontrolü — <c>vw_LeaveBalanceSummary</c> yalnız onaylı
    /// talepleri "kullanılmış" saydığından, bekleyen (henüz onaysız) düşümlü talepleri
    /// de hesaba katar. Böylece çakışmayan birden çok bekleyen talep toplamda bakiyeyi
    /// aşamaz. <paramref name="excludeRequestId"/> onay anında değerlendirilen talebin
    /// kendisini bekleyen toplamdan çıkarmak içindir. Yalnız düşümlü tip için çağrılır.
    /// </summary>
    public static async Task EnsureBalanceAsync(
        IApplicationDbContext context,
        int employeeId,
        int year,
        int requestedDays,
        int? excludeRequestId,
        CancellationToken cancellationToken)
    {
        var summary = await context.LeaveBalanceSummaries.FirstOrDefaultAsync(
            s => s.EmployeeId == employeeId && s.Year == year, cancellationToken);
        var remaining = summary?.RemainingDays ?? 0;

        var pendingDays = await context.LeaveRequests
            .Where(r => r.EmployeeId == employeeId
                        && r.Status == LeaveStatus.Pending
                        && r.StartDate.Year == year
                        && r.LeaveType!.DeductsFromAnnualBalance
                        && (excludeRequestId == null || r.Id != excludeRequestId))
            .SumAsync(r => r.Days, cancellationToken);

        var available = remaining - pendingDays;
        if (requestedDays > available)
        {
            throw new ConflictException(
                $"Yetersiz izin bakiyesi: talep {requestedDays} gün, kullanılabilir {available} gün " +
                $"(kalan {remaining} − bekleyen {pendingDays}).");
        }
    }
}
