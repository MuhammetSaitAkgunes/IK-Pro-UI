using FluentAssertions;
using IKPro.Application.Common.Models;
using IKPro.Application.Features.Employees;
using IKPro.Domain.Entities.Organization;
using IKPro.Domain.Enums;
using System.Net;

namespace IKPro.Tests.Integration.Tenancy;

/// <summary>
/// Faz 3: evrak/dosya erişimi de kiracıya izole. İndirme/liste uçları evrak ve
/// personel kayıtlarını global-filtreli EF üzerinden çözdüğü için, bir kiracı
/// diğerinin evrakına ID tahminiyle erişemez (yabancı ID → 404).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantFileIsolationTests(IKProApiFactory factory) : TenancyTestBase(factory)
{
    [Fact]
    public async Task Documents_AreNotAccessibleAcrossTenants()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@files.local";
        var tenant = await ProvisionTenantAsync("Files A.Ş.", adminEmail);

        // Globex kiracısında bir personel + evrak (fiziksel dosya gerekmez; erişim kontrolü test edilir).
        var dept = new Department { Name = "Files Ekibi" };
        var employee = new Employee
        {
            FirstName = "Zed", LastName = "Zephyr", Title = "Dev",
            Department = dept, HireDate = new DateOnly(2026, 1, 1),
            Status = EmployeeStatus.Active, Profile = new EmployeeProfile(),
        };
        var document = new EmployeeDocument
        {
            Employee = employee, DocumentType = "Sözleşme", FileName = "gizli.pdf",
            FilePath = "files/gizli.pdf", ContentType = "application/pdf", SizeBytes = 42,
        };
        await SeedInTenantAsync(tenant.TenantId, db =>
        {
            db.AddRange(dept, employee, document);
            return Task.CompletedTask;
        });

        // Bu kiracının admin'i kendi personelinin evrakını LİSTELEYEBİLİR (pozitif kontrol).
        var owner = await AuthedClientAsync(adminEmail);
        var ownDocs = await GetAsync<List<EmployeeDocumentDto>>(
            owner, $"/api/employees/{employee.Id}/documents");
        ownDocs.Should().ContainSingle().Which.FileName.Should().Be("gizli.pdf");

        // Varsayılan kiracının admin'i bu personeli/evrakı GÖREMEZ (yabancı ID → 404).
        var demoAdmin = await AuthedClientAsync("ik@hrmaster.local");
        (await demoAdmin.GetAsync($"/api/employees/{employee.Id}/documents"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "yabancı kiracının personeli görünmemeli");
        (await demoAdmin.GetAsync($"/api/employees/{employee.Id}/documents/{document.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound, "yabancı kiracının evrakı indirilememeli");
    }
}
