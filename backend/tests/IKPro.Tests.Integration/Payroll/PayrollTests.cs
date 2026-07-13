using FluentAssertions;
using IKPro.Application.Common.Models;
using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Employees;
using IKPro.Application.Features.Payroll;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IKPro.Tests.Integration.Payroll;

/// <summary>
/// Faz 6 uçtan uca: dönem oluşturma (puantajdan fazla mesai beslemesi), girdi →
/// check → onay → submit yaşam döngüsü, kümülatif matrah devri, dönem özeti (SQL view),
/// bordro pusulası PDF'i ve yetki korumaları. Motor değerleri parite testlerindeki
/// JS referanslarıyla aynı girdiler üzerinden doğrulanır (Ahmet = pr-001 girdileri).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PayrollTests(IKProApiFactory factory)
{
    private const string DemoPassword = "demo123";

    [Fact]
    public async Task Settings_AreSeeded_AndVersionedByYear()
    {
        var admin = await AuthedClientAsync("ik@hrmaster.local");

        var current = await GetAsync<PayrollSettingsDto>(admin, "/api/payroll/settings?year=2026");
        current.OvertimeMultiplier.Should().Be(1.5m);
        current.SgkBaseMin.Should().Be(33030m);
        current.TaxBrackets.Should().HaveCount(5);
        current.TaxBrackets[^1].Limit.Should().BeNull("son dilim sınırsız");

        // 2027 versiyonu oluştur: çarpan 2.0; 2026 etkilenmez.
        var updateResponse = await admin.PutAsJsonAsync("/api/payroll/settings", new
        {
            year = 2027,
            overtimeMultiplier = 2.0m,
            monthlyWorkingHours = current.MonthlyWorkingHours,
            defaultWorkedDays = current.DefaultWorkedDays,
            sgkEmployeeRate = current.SgkEmployeeRate,
            unemploymentEmployeeRate = current.UnemploymentEmployeeRate,
            sgkEmployerRate = current.SgkEmployerRate,
            unemploymentEmployerRate = current.UnemploymentEmployerRate,
            stampTaxRate = current.StampTaxRate,
            sgkBaseMin = current.SgkBaseMin,
            sgkBaseMax = current.SgkBaseMax,
            monthlyMinWageIncomeTaxExemption = current.MonthlyMinWageIncomeTaxExemption,
            monthlyMinWageStampTaxExemption = current.MonthlyMinWageStampTaxExemption,
            minWageGross = current.MinWageGross,
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await GetAsync<PayrollSettingsDto>(admin, "/api/payroll/settings?year=2027"))
            .OvertimeMultiplier.Should().Be(2.0m);
        (await GetAsync<PayrollSettingsDto>(admin, "/api/payroll/settings?year=2026"))
            .OvertimeMultiplier.Should().Be(1.5m);

        // Yönetim rolleri dışında kalanlar ve manager ayarlara erişemez.
        var manager = await AuthedClientAsync("ece.arslan@hrmaster.local");
        (await manager.GetAsync("/api/payroll/settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Preview_MatchesJsParityScenario()
    {
        var admin = await AuthedClientAsync("ik@hrmaster.local");

        var response = await admin.PostAsJsonAsync("/api/payroll/preview", new
        {
            grossSalary = 85000,
            workedDays = 30,
            overtimeHours = 10,
            overtimeMultiplier = 2,
            premiumPay = 1500,
            roadAllowance = 800,
            mealAllowance = 1200,
            specialDeductions = 500,
            previousTaxBase = 185000,
            year = 2026,
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var calc = (await response.Content.ReadFromJsonAsync<PayrollCalcDto>())!;
        calc.NetPay.Should().BeApproximately(68800.416111m, 0.01m, "JS motor referans değeri");
        calc.EmployerCost.Should().BeApproximately(117668.055556m, 0.01m);
        calc.Warnings.Should().Contain("Vergi dilimi geçişi");
    }

    [Fact]
    public async Task PeriodLifecycle_EndToEnd_WithCumulativeCarryAndPayslip()
    {
        var admin = await AuthedClientAsync("ik@hrmaster.local");
        var ahmetEmployeeId = await FindEmployeeIdAsync(admin, "Ahmet");

        // Eylül 2026 puantajı: Ahmet 09:00–20:00 → 120 dk fazla mesai (bordroya beslenecek).
        (await admin.PostAsJsonAsync("/api/attendance", new
        {
            employeeId = ahmetEmployeeId,
            model = new { workDate = "2026-09-01", checkIn = "09:00", checkOut = "20:00", breakMinutes = 60 },
        })).StatusCode.Should().Be(HttpStatusCode.Created);

        // --- dönem oluştur ---
        var createResponse = await admin.PostAsJsonAsync("/api/payroll/periods", new { year = 2026, month = 9 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var period = (await createResponse.Content.ReadFromJsonAsync<PayrollPeriodDetailDto>())!;

        period.Name.Should().Be("Eylül 2026");
        period.Status.Should().Be("draft");
        period.Rows.Should().NotBeEmpty("aktif personel için girdi satırları üretilir");
        period.Rows.Select(r => r.Name).Should().Contain(["Ahmet Yılmaz", "Ece Arslan", "Ayşe Demir"]);
        period.Rows.Select(r => r.Name).Should().NotContain("Selin Koç", "pasif personel döneme girmez");

        var ahmetRow = period.Rows.Single(r => r.Name == "Ahmet Yılmaz");
        ahmetRow.OvertimeHours.Should().Be(2, "puantaj özetindeki 120 dk fazla mesai saate çevrilir");
        ahmetRow.TimesheetComplete.Should().BeTrue();
        ahmetRow.ApprovalStatus.Should().Be("Ön Hesap");

        (await admin.PostAsJsonAsync("/api/payroll/periods", new { year = 2026, month = 9 }))
            .StatusCode.Should().Be(HttpStatusCode.Conflict, "aynı dönem ikinci kez açılamaz");

        // --- Ahmet girdileri = parite senaryosu pr-001 ---
        var updateResponse = await admin.PutAsJsonAsync(
            $"/api/payroll/periods/{period.Id}/rows/{ahmetRow.Id}", new
            {
                grossSalary = 118000,
                workedDays = 30,
                overtimeHours = 12,
                premiumPay = 3500,
                roadAllowance = 1200,
                mealAllowance = 1800,
                benefitPay = 3200,
                specialDeductions = 1800,
                previousTaxBase = 312000,
                ibanComplete = true,
                timesheetComplete = true,
            });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedRow = (await updateResponse.Content.ReadFromJsonAsync<PayrollRowDto>())!;

        updatedRow.ApprovalStatus.Should().Be("Kontrol");
        updatedRow.NetPay.Should().BeApproximately(92876.1774m, 0.01m, "JS motor pr-001 referansı");
        updatedRow.EmployerCost.Should().BeApproximately(167996.5m, 0.01m);
        updatedRow.Warnings.Should().Contain("Kontrol bekliyor");

        // --- check: eksiksiz satırlar Onaya Hazır, IBAN'sızlar Eksik Veri ---
        var checkResponse = await admin.PostAsync($"/api/payroll/periods/{period.Id}/check", null);
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkedPeriod = (await checkResponse.Content.ReadFromJsonAsync<PayrollPeriodDetailDto>())!;
        checkedPeriod.Status.Should().Be("control");
        checkedPeriod.Rows.Single(r => r.Id == ahmetRow.Id).ApprovalStatus.Should().Be("Onaya Hazır");

        var missingRow = checkedPeriod.Rows.FirstOrDefault(r => r.ApprovalStatus == "Eksik Veri");
        if (missingRow is not null)
        {
            (await admin.PostAsync(
                    $"/api/payroll/periods/{period.Id}/rows/{missingRow.Id}/approve", null))
                .StatusCode.Should().Be(HttpStatusCode.Conflict, "eksik verili satır onaylanamaz");
        }

        // Tüm satırlar onaylanmadan submit 409.
        (await admin.PostAsync($"/api/payroll/periods/{period.Id}/submit", null))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);

        // --- kalan satırları tamamla ve tümünü onayla ---
        foreach (var row in checkedPeriod.Rows.Where(r => r.Id != ahmetRow.Id))
        {
            (await admin.PutAsJsonAsync($"/api/payroll/periods/{period.Id}/rows/{row.Id}", new
            {
                grossSalary = 50000,
                workedDays = 30,
                overtimeHours = 0,
                ibanComplete = true,
                timesheetComplete = true,
            })).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        foreach (var row in checkedPeriod.Rows)
        {
            (await admin.PostAsync($"/api/payroll/periods/{period.Id}/rows/{row.Id}/approve", null))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // --- submit → dönem onaylanır ve kilitlenir ---
        var submitResponse = await admin.PostAsync($"/api/payroll/periods/{period.Id}/submit", null);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await submitResponse.Content.ReadFromJsonAsync<PayrollPeriodDetailDto>())!
            .Status.Should().Be("approved");

        (await admin.PutAsJsonAsync($"/api/payroll/periods/{period.Id}/rows/{ahmetRow.Id}", new
        {
            grossSalary = 1,
            workedDays = 30,
            overtimeHours = 0,
        })).StatusCode.Should().Be(HttpStatusCode.Conflict, "kapanan dönem satırı değişmez");

        // --- özet (SQL view) ---
        var summary = await GetAsync<PeriodSummaryDto>(admin, $"/api/payroll/periods/{period.Id}/summary");
        summary.EmployeeCount.Should().Be(checkedPeriod.Rows.Count);
        summary.ApprovedCount.Should().Be(checkedPeriod.Rows.Count);
        summary.TotalNet.Should().BeGreaterThan(90000m);

        // --- kümülatif matrah devri: Ekim dönemi Ahmet satırı ---
        var finalPeriod = await GetAsync<PayrollPeriodDetailDto>(admin, $"/api/payroll/periods/{period.Id}");
        var approvedAhmet = finalPeriod.Rows.Single(r => r.Id == ahmetRow.Id);

        var octoberResponse = await admin.PostAsJsonAsync("/api/payroll/periods", new { year = 2026, month = 10 });
        octoberResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var october = (await octoberResponse.Content.ReadFromJsonAsync<PayrollPeriodDetailDto>())!;

        october.Rows.Single(r => r.Name == "Ahmet Yılmaz").PreviousTaxBase
            .Should().BeApproximately(312000m + approvedAhmet.IncomeTaxBase, 0.01m,
                "önceki dönemin GV matrahı kümülatife devrolur");

        // --- pusula PDF: çalışan kendisininkini indirir, başkasınınkini indiremez ---
        var employee = await AuthedClientAsync("ahmet.yilmaz@hrmaster.local");

        var payslipResponse = await employee.GetAsync(
            $"/api/payroll/periods/{period.Id}/rows/{ahmetRow.Id}/payslip");
        payslipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        payslipResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var pdfBytes = await payslipResponse.Content.ReadAsByteArrayAsync();
        pdfBytes.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(pdfBytes[..4]).Should().Be("%PDF");

        var eceRowId = checkedPeriod.Rows.Single(r => r.Name == "Ece Arslan").Id;
        (await employee.GetAsync($"/api/payroll/periods/{period.Id}/rows/{eceRowId}/payslip"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // --- /my: çalışan kendi onaylı bordrolarını görür ---
        var myPayslips = await GetAsync<List<MyPayslipDto>>(employee, "/api/payroll/my");
        myPayslips.Should().Contain(p =>
            p.PeriodName == "Eylül 2026" && Math.Abs(p.NetPay - 92876.1774m) < 0.01m);
    }

    [Fact]
    public async Task AccessGuards_EmployeeAndManagerBlockedFromPeriodManagement()
    {
        var employee = await AuthedClientAsync("ahmet.yilmaz@hrmaster.local");
        (await employee.GetAsync("/api/payroll/periods")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await employee.GetAsync("/api/payroll/settings")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // routes.js: /payroll rolleri hr-admin + employee; manager tamamen dışarıda.
        var manager = await AuthedClientAsync("ece.arslan@hrmaster.local");
        (await manager.GetAsync("/api/payroll/my")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // --- yardımcılar ve yanıt modelleri ---

    private sealed record PayrollCalcDto(decimal NetPay, decimal EmployerCost, List<string> Warnings);

    private sealed record PeriodSummaryDto(
        int PayrollPeriodId, int EmployeeCount, int ApprovedCount,
        decimal TotalGross, decimal TotalNet, decimal TotalDeductions, decimal TotalEmployerCost);

    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var anonymous = factory.CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password = DemoPassword });
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"demo giriş başarısız: {email}");
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private static async Task<T> GetAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"GET {url}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<int> FindEmployeeIdAsync(HttpClient adminClient, string search)
    {
        var response = await adminClient.GetAsync($"/api/employees?search={Uri.EscapeDataString(search)}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<PagedResult<EmployeeListItemDto>>())!;
        return page.Items.First().Id;
    }
}
