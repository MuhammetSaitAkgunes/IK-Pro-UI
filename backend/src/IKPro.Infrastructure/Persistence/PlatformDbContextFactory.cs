using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IKPro.Infrastructure.Persistence;

/// <summary>
/// Tasarım zamanı (dotnet ef) platform context üreticisi. Bağlantı dizesi
/// IKPRO_PLATFORM_CONNECTION ortam değişkeninden ya da varsayılan yerel
/// MSSQL'den alınır (migration üretimi için içerik önemsizdir).
/// </summary>
public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("IKPRO_PLATFORM_CONNECTION")
            ?? "Server=localhost;Database=IKProPlatform;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory_Platform");
            })
            .Options;

        return new PlatformDbContext(options);
    }
}
