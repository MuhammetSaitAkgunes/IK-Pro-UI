using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Dashboard.Queries;

/// <summary>
/// Uyum risk merkezi özeti (dashboard.js complianceMetrics karşılığı).
/// Skorlar ComplianceDocuments tablosundan hesaplanır:
///   evrak uyum skoru = tamamlanan / toplam;
///   denetim hazırlık = 100 - eksik*6 - süresiYaklaşan*3 - incelemede*2 (0..100).
/// Faz 9'da durum iş akışı + hazırlık skoru view'ı bu ucu besleyecek şekilde genişler.
/// </summary>
public sealed record GetComplianceRiskQuery : IRequest<ComplianceRiskDto>;

public sealed class GetComplianceRiskQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetComplianceRiskQuery, ComplianceRiskDto>
{
    public async Task<ComplianceRiskDto> Handle(
        GetComplianceRiskQuery request, CancellationToken cancellationToken)
    {
        var documents = await context.ComplianceDocuments
            .Include(d => d.Employee)!.ThenInclude(e => e!.Department)
            .OrderBy(d => d.DueDate == null).ThenBy(d => d.DueDate)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var total = documents.Count;
        var completed = documents.Count(d => d.Status == ComplianceStatus.Completed);
        var missing = documents.Count(d => d.Status == ComplianceStatus.Missing);
        var dueSoon = documents.Count(d => d.Status == ComplianceStatus.DueSoon);
        var inReview = documents.Count(d => d.Status == ComplianceStatus.InReview);

        var complianceScore = total == 0 ? 100 : (int)Math.Round(100.0 * completed / total);
        var readinessScore = Math.Clamp(100 - missing * 6 - dueSoon * 3 - inReview * 2, 0, 100);
        var readinessRisk = readinessScore >= 80 ? "Düşük" : readinessScore >= 60 ? "Orta" : "Yüksek";

        var records = documents
            .Select(d => new ComplianceRecordDto(
                d.Id,
                d.Employee?.FullName ?? string.Empty,
                d.Employee?.Department?.Name ?? string.Empty,
                d.DocumentName,
                d.OwnerName,
                DueLabel(d.Status, d.DueDate, today),
                StatusLabel(d.Status),
                LevelLabel(d.Level)))
            .ToList();

        // Yaklaşan son tarihler: tamamlanmamış evraklar belge adına göre gruplanır.
        var deadlines = documents
            .Where(d => d.Status != ComplianceStatus.Completed)
            .GroupBy(d => d.DocumentName)
            .Select(g =>
            {
                var earliest = g.OrderBy(d => d.DueDate == null).ThenBy(d => d.DueDate).First();
                var level = g.Any(d => d.Level == RiskLevel.High) ? "high"
                    : g.Any(d => d.Level == RiskLevel.Medium) ? "medium" : "low";
                return new ComplianceDeadlineDto(
                    g.Key, g.Count(), DueLabel(earliest.Status, earliest.DueDate, today),
                    earliest.OwnerName, level);
            })
            .OrderBy(d => d.Level == "high" ? 0 : d.Level == "medium" ? 1 : 2)
            .ToList();

        return new ComplianceRiskDto(
            complianceScore, missing, dueSoon, readinessRisk, readinessScore,
            records, deadlines);
    }

    /// <summary>Frontend son tarih etiketi: Tamamlandı | Bugün | Gecikti | "N gün".</summary>
    private static string DueLabel(ComplianceStatus status, DateOnly? dueDate, DateOnly today)
    {
        if (status == ComplianceStatus.Completed) return "Tamamlandı";
        if (dueDate is null) return "-";
        if (dueDate.Value == today) return "Bugün";
        if (dueDate.Value < today) return "Gecikti";
        return $"{dueDate.Value.DayNumber - today.DayNumber} gün";
    }

    /// <summary>Enum kataloğu (plan Ek A): Eksik | İncelemede | Süresi Yaklaşıyor | Tamamlandı.</summary>
    private static string StatusLabel(ComplianceStatus status) => status switch
    {
        ComplianceStatus.Missing => "Eksik",
        ComplianceStatus.InReview => "İncelemede",
        ComplianceStatus.DueSoon => "Süresi Yaklaşıyor",
        ComplianceStatus.Completed => "Tamamlandı",
        _ => status.ToString(),
    };

    private static string LevelLabel(RiskLevel level) => level switch
    {
        RiskLevel.High => "high",
        RiskLevel.Medium => "medium",
        _ => "low",
    };
}
