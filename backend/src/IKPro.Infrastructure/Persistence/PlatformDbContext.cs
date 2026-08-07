using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Infrastructure.Persistence;

/// <summary>
/// Platform veritabanının (IKProPlatform) context'i. Yalnız kiracı kimliğini
/// tutar; İK verisi <see cref="AppDbContext"/> tarafındadır.
///
/// Migration'ları AYRI bir klasörde tutulur (Migrations/Platform) ve ayrı bir
/// __EFMigrationsHistory tablosuna yazılır — iki context'in geçmişi birbirine
/// karışmaz.
/// </summary>
public class PlatformDbContext(DbContextOptions<PlatformDbContext> options)
    : DbContext(options), IPlatformDbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantDirectoryEntry> Directory => Set<TenantDirectoryEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(b =>
        {
            b.Property(t => t.Name).IsRequired().HasMaxLength(200);
            b.Property(t => t.Slug).IsRequired().HasMaxLength(64);
            b.HasIndex(t => t.Slug).IsUnique();

            // Okunabilirlik: durum veritabanında metin olarak durur (AppDbContext ile aynı kural).
            b.Property(t => t.Status).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<TenantDirectoryEntry>(b =>
        {
            b.HasKey(d => d.NormalizedEmail);
            b.Property(d => d.NormalizedEmail).HasMaxLength(256);
            b.HasIndex(d => d.TenantId);
        });
    }
}
