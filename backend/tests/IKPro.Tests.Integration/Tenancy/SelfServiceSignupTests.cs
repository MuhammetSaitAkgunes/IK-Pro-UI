using FluentAssertions;
using IKPro.Application.Features.Departments;
using IKPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Self-servis kayıt: public /api/tenants/signup ile şirket+admin oluşturma. Kiracı
/// pasif başlar; admin davet e-postasını kabul edince etkinleşir. İzolasyon korunur.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SelfServiceSignupTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    private Task<HttpResponseMessage> SignupAsync(string company, string email) =>
        Factory.CreateClient().PostAsJsonAsync("/api/tenants/signup", new
        {
            companyName = company,
            adminName = "Kurucu Yönetici",
            adminEmail = email,
        });

    [Fact]
    public async Task Signup_CreatesInactiveTenant_ActivatedOnInviteAccept()
    {
        var email = $"kurucu-{Guid.NewGuid():N}@yenisirket.local";
        var response = await SignupAsync("Yeni Şirket A.Ş.", email);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Doğrulanmadan giriş yapılamaz (kiracı pasif).
        var preLogin = await Factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email, password = "demo123" });
        preLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "doğrulanmamış kiracı pasif");

        // Davet kabulü kiracıyı etkinleştirir → giriş çalışır, yalnız kendi (boş) kiracısını görür.
        await AcceptInviteAsync(email);
        var admin = await AuthedClientAsync(email);
        var depts = await GetAsync<List<DepartmentDto>>(admin, "/api/departments");
        depts.Should().BeEmpty();
    }

    [Fact]
    public async Task Signup_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@x.local";
        (await SignupAsync("İlk Şirket", email)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await SignupAsync("İkinci Şirket", email)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_SameCompanyName_DerivesDistinctSlugs()
    {
        (await SignupAsync("Paralel A.Ş.", $"a-{Guid.NewGuid():N}@p.local"))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await SignupAsync("Paralel A.Ş.", $"b-{Guid.NewGuid():N}@p.local"))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var slugs = await db.Tenants.Where(t => t.Name == "Paralel A.Ş.")
            .Select(t => t.Slug).ToListAsync();
        slugs.Should().HaveCountGreaterThanOrEqualTo(2);
        slugs.Should().OnlyHaveUniqueItems("aynı ad için slug'lar benzersiz türetilmeli");
    }
}
