using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Common;
using IKPro.Domain.Entities.Tenancy;
using IKPro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace IKPro.Infrastructure.Persistence;

/// <summary>
/// Kiracının tüm verisini FK-güvenli sırada siler. Silinecek tablolar EF model
/// metadata'sından türetilir (ITenantScoped + PK'lı = view'ler hariç); yeni bir
/// kiracı-kapsamlı tablo eklendiğinde otomatik kapsama girer (unutma sızıntısı yok).
/// </summary>
public sealed class TenantPurger(
    AppDbContext context,
    IPlatformDbContext platform,
    ICurrentTenant currentTenant,
    IFileStorage fileStorage,
    ILogger<TenantPurger> logger) : ITenantPurger
{
    public async Task PurgeAsync(int tenantId, CancellationToken cancellationToken)
    {
        // Global filtre için impersone et: aradaki sorgular kiracı filtresine tabidir.
        // Dosya yollarını önceden toplamaya gerek yok — silme, kiracının tüm alanı üzerinden
        // yapılır (bkz. adım 4).
        currentTenant.Impersonate(tenantId);

        var tables = TenantScopedTablesInDeleteOrder();

        // Tablo adları metadata'dan çözülür (Identity tabloları yeniden adlandırılmış: Users/UserRoles…).
        var refreshTokenTable = QualifiedTableName(typeof(RefreshToken));
        var userTable = QualifiedTableName(typeof(ApplicationUser));

        await using var tx = await context.Database.BeginTransactionAsync(cancellationToken);

        // EF1002 bilinçli olarak bastırılıyor: tenantId SQL'e PARAMETRE olarak geçiyor
        // ({0} yer tutucusu + object[]), dolayısıyla enjeksiyona kapalı. Enterpole edilen
        // tek şey EF metadata'sından çözülen tablo adları — tablo adı SQL'de zaten
        // parametreleştirilemez ve kullanıcı girdisinden gelmez.
#pragma warning disable EF1002
        // 1) ITenantScoped tablolar (çocuk→ebeveyn). Açık TenantId filtresi (impersonation'a bağlı değil).
        foreach (var table in tables)
        {
            await context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM {table} WHERE [TenantId] = {{0}}", new object[] { tenantId }, cancellationToken);
        }

        // 2) Kimlik: refresh token'lar, sonra kullanıcılar (UserRoles/UserClaims cascade).
        await context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM {refreshTokenTable} WHERE [TenantId] = {{0}}", new object[] { tenantId }, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            $"DELETE FROM {userTable} WHERE [TenantId] = {{0}}", new object[] { tenantId }, cancellationToken);
#pragma warning restore EF1002

        await tx.CommitAsync(cancellationToken);

        // 3) Kiracı satırı platform veritabanındadır — uygulama transaction'ı onu kapsamaz.
        // Bu yüzden ayrı silinir. Buradan önce patlarsa kiracı satırı kalır ama verisi
        // gitmiştir; durum Purging'de kaldığı için kiracı erişilemez olarak durur
        // (bkz. Task 6) ve operatör yeniden çalıştırabilir.
        var tenantRow = await platform.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenantRow is not null)
        {
            platform.Tenants.Remove(tenantRow);
            await platform.SaveChangesAsync(cancellationToken);
        }

        // 4) Fiziksel dosyalar (DB tutarlılığından SONRA — rollback olursa dosya kaybı olmasın).
        // Kiracının TÜM dosya alanı silinir. Eskiden yalnız EmployeeDocuments yolları tek tek
        // siliniyordu; bu, çalışan fotoğraflarını ve şirket logosunu KAÇIRIYORDU. Alan silme
        // ileride eklenecek her dosya türünü de otomatik kapsar.
        // Hata sessizce yutulmaz: DB temizlenip dosyalar kalırsa KVKK açısından PII riski
        // sürer, elle temizlik için LOUD loglanır. Hata purge'ü yarıda kesmez.
        try
        {
            await fileStorage.DeleteTenantSpaceAsync(tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Kiracı {TenantId} purge: dosya alanı silinemedi — elle temizlik gerekiyor.",
                tenantId);
        }
    }

    public async Task<int> PurgeUnverifiedAsync(DateTime createdBeforeUtc, CancellationToken cancellationToken)
    {
        // Doğrulanmamış + eski kiracı adayları (Tenant filtresizdir).
        var candidateIds = await platform.Tenants
            .Where(t => t.Status == TenantStatus.Provisioning && t.CreatedAtUtc < createdBeforeUtc)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);
        if (candidateIds.Count == 0) return 0;

        // Şifre belirlemiş (davet kabul edilmiş / askıya alınmış) kullanıcısı olan kiracıları hariç tut.
        var verifiedTenantIds = await context.Set<ApplicationUser>()
            .Where(u => u.PasswordHash != null && candidateIds.Contains(u.TenantId))
            .Select(u => u.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var toPurge = candidateIds.Except(verifiedTenantIds).ToList();
        foreach (var id in toPurge)
        {
            await PurgeAsync(id, cancellationToken);
        }
        return toPurge.Count;
    }

    // ITenantScoped + tablo (PK'lı) tipleri FK bağımlılığına göre çocuk-önce sıralar.
    private List<string> TenantScopedTablesInDeleteOrder()
    {
        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType)
                        && e.FindPrimaryKey() is not null // keyless view'leri (read-model) dışla
                        && e.GetTableName() is not null)
            .Distinct()
            .ToList();

        var set = entityTypes.ToHashSet();
        var visited = new HashSet<IEntityType>();
        var ordered = new List<IEntityType>();

        void Visit(IEntityType node)
        {
            if (!visited.Add(node)) return;
            // node'un referans ettiği (principal) kiracı-kapsamlı tipleri önce ekle;
            // böylece 'ordered' principal-önce olur, tersine çevirince child-önce.
            foreach (var fk in node.GetForeignKeys())
            {
                var principal = fk.PrincipalEntityType;
                if (principal != node && set.Contains(principal))
                {
                    Visit(principal);
                }
            }
            ordered.Add(node);
        }

        foreach (var e in entityTypes) Visit(e);

        ordered.Reverse(); // principal-önce → child-önce (silme için)

        return ordered
            .Select(e => e.GetSchema() is { } s ? $"[{s}].[{e.GetTableName()}]" : $"[{e.GetTableName()}]")
            .Distinct()
            .ToList();
    }

    // Bir CLR tipinin köşeli-parantezli, şema-nitelikli tablo adını metadata'dan üretir.
    private string QualifiedTableName(Type clrType)
    {
        var entityType = context.Model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"{clrType.Name} için EF entity tipi bulunamadı.");
        var table = entityType.GetTableName()
            ?? throw new InvalidOperationException($"{clrType.Name} bir tabloya eşlenmemiş.");
        return entityType.GetSchema() is { } schema ? $"[{schema}].[{table}]" : $"[{table}]";
    }
}
