using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Login, kiracıyı yönlendirme dizininden çözer. Faz 2'de kullanıcı tablosu
/// kiracı veritabanında olacağı için, hangi veritabanına bakılacağını bilmeden
/// kullanıcı aranamaz — dizin bu yüzden login'in ÖN adımıdır.
/// </summary>
[Collection(ApiCollection.Name)]
public class LoginDizinTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task DizindeOlmayanEposta_GirisYapamaz()
    {
        var eposta = $"dizinsiz-{Guid.NewGuid():N}@ornek.local";
        var kiraci = await ProvisionAndActivateAsync("LoginDizin", eposta);

        // Dizin kaydını sil: kullanıcı Identity'de duruyor ama yönlendirme yok.
        using (var scope = Factory.Services.CreateScope())
        {
            var platform = scope.ServiceProvider.GetRequiredService<IPlatformDbContext>();
            var anahtar = TenantDirectoryEntry.Normalize(eposta);
            var kayit = await platform.Directory.FirstAsync(d => d.NormalizedEmail == anahtar);
            platform.Directory.Remove(kayit);
            await platform.SaveChangesAsync(default);
        }

        var yanit = await Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email = eposta, password = DefaultPassword });

        yanit.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "dizin yönlendirmesi olmadan hangi kiracıya bakılacağı bilinemez");

        // Kurtarma yolu çalışmalı: dizin yeniden kurulunca giriş geri gelmeli.
        var platformClient = Factory.CreateClient();
        platformClient.DefaultRequestHeaders.Add("X-Platform-Key", IKProApiFactory.PlatformKey);
        (await platformClient.PostAsync($"/api/tenants/{kiraci.TenantId}/rebuild-directory", null))
            .EnsureSuccessStatusCode();

        var ikinciDeneme = await Factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new { email = eposta, password = DefaultPassword });

        ikinciDeneme.StatusCode.Should().Be(HttpStatusCode.OK,
            "dizin yeniden kurulduktan sonra giriş çalışmalı — kurtarma prosedürünün kanıtı");
    }
}
