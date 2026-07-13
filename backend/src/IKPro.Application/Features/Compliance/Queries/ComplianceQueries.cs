using IKPro.Application.Common.Interfaces;
using IKPro.Domain.ReadModels;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Compliance.Queries;

/// <summary>
/// Uyum belgeleri listesi: durum/seviye/arama filtreli, rol kapsamlı
/// (hr-admin → hepsi, manager → ekibi + kendisi).
/// </summary>
public sealed record GetComplianceDocumentsQuery(
    string? Status = null,
    string? Level = null,
    string? Search = null) : IRequest<IReadOnlyList<ComplianceDocumentDto>>;

public sealed class GetComplianceDocumentsQueryHandler(
    IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetComplianceDocumentsQuery, IReadOnlyList<ComplianceDocumentDto>>
{
    public async Task<IReadOnlyList<ComplianceDocumentDto>> Handle(
        GetComplianceDocumentsQuery request, CancellationToken cancellationToken)
    {
        var query = context.ComplianceDocuments
            .Include(d => d.Employee)!.ThenInclude(e => e!.Department)
            .ScopeFor(currentUser);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ComplianceMappings.ParseStatus(request.Status);
            query = query.Where(d => d.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Level))
        {
            var level = ComplianceMappings.ParseLevel(request.Level);
            query = query.Where(d => d.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(d =>
                d.DocumentName.Contains(term) ||
                (d.Employee!.FirstName + " " + d.Employee.LastName).Contains(term) ||
                (d.OwnerName != null && d.OwnerName.Contains(term)));
        }

        var documents = await query
            .OrderBy(d => d.DueDate == null).ThenBy(d => d.DueDate)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return documents.Select(d => d.ToDto(today)).ToList();
    }
}

/// <summary>Denetim hazırlık özeti: vw_ComplianceReadiness + türetilmiş kontrol listesi.</summary>
public sealed record GetComplianceReadinessQuery : IRequest<ComplianceReadinessDto>;

public sealed class GetComplianceReadinessQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetComplianceReadinessQuery, ComplianceReadinessDto>
{
    public async Task<ComplianceReadinessDto> Handle(
        GetComplianceReadinessQuery request, CancellationToken cancellationToken)
    {
        var readiness = await context.ComplianceReadiness
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ComplianceReadiness { DocumentComplianceScore = 100, ReadinessScore = 100 };

        return readiness.ToDto();
    }
}

public static class ComplianceReadinessMappings
{
    public static ComplianceReadinessDto ToDto(this ComplianceReadiness r)
    {
        // Oranlar boş tabloda %100 kabul edilir (risk yok).
        var dueSoonControl = r.TotalCount == 0
            ? 100 : 100 - (int)Math.Round(100.0 * r.DueSoonCount / r.TotalCount);
        var ownerClarity = r.TotalCount == 0
            ? 100 : (int)Math.Round(100.0 * r.OwnedCount / r.TotalCount);

        // dashboard.js auditChecklist kalemlerinin veri kaynaklı karşılıkları.
        var checklist = new List<AuditChecklistItemDto>
        {
            new("Personel dosyası bütünlüğü", r.DocumentComplianceScore,
                ChecklistLevel(r.DocumentComplianceScore)),
            new("Süresi yaklaşan evrak kontrolü", dueSoonControl, ChecklistLevel(dueSoonControl)),
            new("Sorumlu atama netliği", ownerClarity, ChecklistLevel(ownerClarity)),
            new("Denetim klasörü hazırlığı", r.ReadinessScore, ChecklistLevel(r.ReadinessScore)),
        };

        var recommendedActions = new List<string>();
        if (r.MissingCount > 0)
        {
            recommendedActions.Add($"Eksik {r.MissingCount} evrakı bugün kapat");
        }
        if (r.DueSoonCount > 0)
        {
            recommendedActions.Add($"Süresi yaklaşan {r.DueSoonCount} evrak için sorumlu ataması yap");
        }
        if (r.OwnedCount < r.TotalCount)
        {
            recommendedActions.Add("Sorumlusu olmayan evraklara owner ata");
        }
        if (recommendedActions.Count == 0)
        {
            recommendedActions.Add("Denetim klasörü kontrol listesini haftalık takip et");
        }

        return new ComplianceReadinessDto(
            r.TotalCount,
            r.CompletedCount,
            r.MissingCount,
            r.DueSoonCount,
            r.InReviewCount,
            r.OwnedCount,
            r.DocumentComplianceScore,
            r.ReadinessScore,
            r.ReadinessScore >= 80 ? "Düşük" : r.ReadinessScore >= 60 ? "Orta" : "Yüksek",
            checklist,
            recommendedActions);
    }

    private static string ChecklistLevel(int value)
        => value >= 85 ? "low" : value >= 70 ? "medium" : "high";
}
