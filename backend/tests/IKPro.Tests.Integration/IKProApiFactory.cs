using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;

namespace IKPro.Tests.Integration;

/// <summary>
/// Entegrasyon testleri için API fabrikası. Development ortamında ayağa kalkar;
/// Program.cs migration + demo seed'i otomatik uygular. Deterministik koşu için
/// test veritabanı her fabrika oluşumunda sıfırdan kurulur.
///
/// ÖNEMLİ: Bağlantı dizesi ortam değişkeniyle geçilir. Minimal hosting'de
/// (WebApplication.CreateBuilder) fabrikanın ConfigureAppConfiguration kaynakları
/// uygulamanın kendi appsettings.*.json kaynaklarından ÖNCE geldiği için ezilir;
/// ortam değişkenleri ise appsettings'ten sonra eklenir ve her zaman kazanır.
/// </summary>
public sealed class IKProApiFactory : WebApplicationFactory<Program>
{
    private const string TestDatabaseName = "IKProDb_Test";

    /// <summary>Testlerde kiracı provizyon ucunu çağırmak için platform anahtarı.</summary>
    public const string PlatformKey = "test-platform-key";

    /// <summary>Davet e-postalarının (outbox) yazıldığı kök — davet token'ını okumak için.</summary>
    public string StorageRoot { get; }

    private const string TestConnectionString =
        $"Server=localhost;Database={TestDatabaseName};Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";

    public IKProApiFactory()
    {
        StorageRoot = Path.Combine(Path.GetTempPath(), "ikpro-test-storage", Guid.NewGuid().ToString("N"));

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", TestConnectionString);
        Environment.SetEnvironmentVariable("Platform__ProvisioningKey", PlatformKey);
        // Testler çok sayıda login/provizyon/kayıt yapar → rate limit'leri etkisiz kıl.
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitPerMinute", "1000000");
        Environment.SetEnvironmentVariable("RateLimiting__SignupPermitPerHour", "1000000");
        Environment.SetEnvironmentVariable("Storage__Root", StorageRoot);

        DropTestDatabase();
    }

    private static void DropTestDatabase()
    {
        using var connection = new SqlConnection(
            "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{TestDatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{TestDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{TestDatabaseName}];
            END
            """;
        command.ExecuteNonQuery();
    }
}

/// <summary>
/// Tüm API test sınıfları tek koleksiyonda koşar: tek host + tek test veritabanı,
/// sınıflar arası paralel koşu çakışması olmaz.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<IKProApiFactory>
{
    public const string Name = "api";
}
