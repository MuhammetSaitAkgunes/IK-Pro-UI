using FluentAssertions;
using IKPro.Infrastructure.Tenancy;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IKPro.Tests.Unit.Tenancy;

/// <summary>
/// Bağlantı çözücü, kiracıdan veritabanına giden ayrılma noktasıdır.
/// Faz 1b'de HERKESE aynı dizeyi döndürür — tesisat kurulur ama veri bölünmez.
/// Faz 2'de burası kiracının katalog adını üretecek.
/// </summary>
public class BaglantiCozucuTests
{
    private const string Dize = "Server=localhost;Database=IKProDb;Trusted_Connection=True;";

    private static TenantConnectionResolver Cozucu() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = Dize,
            })
            .Build());

    [Fact]
    public void Cozucu_BuFazdaTumKiracilaraAyniDizeyiDoner()
    {
        var cozucu = Cozucu();

        cozucu.ResolveFor(1).Should().Be(Dize);
        cozucu.ResolveFor(2).Should().Be(Dize);
        cozucu.ResolveFor(null).Should().Be(Dize,
            "kiracı bağlamı olmayan işler (migration, platform işlemleri) de çalışabilmeli");
    }

    [Fact]
    public void Cozucu_BaglantiDizesiYoksaAnlamliHataVerir()
    {
        var cozucu = new TenantConnectionResolver(new ConfigurationBuilder().Build());

        FluentActions.Invoking(() => cozucu.ResolveFor(1))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*DefaultConnection*");
    }
}
