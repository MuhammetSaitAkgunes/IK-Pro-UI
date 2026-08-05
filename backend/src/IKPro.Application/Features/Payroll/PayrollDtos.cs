using IKPro.Domain.Entities.Payroll;
using IKPro.Domain.Enums;
using IKPro.Domain.Services;

namespace IKPro.Application.Features.Payroll;

public sealed record TaxBracketDto(int Order, decimal? Limit, decimal Base, decimal BaseTax, decimal Rate);

/// <summary>Bordro parametreleri — payroll.js payrollDefaultSettings alanlarıyla birebir.</summary>
public sealed record PayrollSettingsDto(
    DateOnly EffectiveFrom,
    decimal OvertimeMultiplier,
    decimal MonthlyWorkingHours,
    int DefaultWorkedDays,
    decimal SgkEmployeeRate,
    decimal UnemploymentEmployeeRate,
    decimal SgkEmployerRate,
    decimal UnemploymentEmployerRate,
    decimal StampTaxRate,
    decimal SgkBaseMin,
    decimal SgkBaseMax,
    decimal MonthlyMinWageIncomeTaxExemption,
    decimal MonthlyMinWageStampTaxExemption,
    decimal MinWageGross,
    IReadOnlyList<TaxBracketDto> TaxBrackets);

public sealed record PayrollPeriodListItemDto(
    int Id, string Name, int Year, int Month, string Status, int EmployeeCount);

/// <summary>Dönem satırı: girdi + motor çıktısı (onaylı satırlarda kalıcı snapshot).</summary>
public sealed record PayrollRowDto(
    int Id,
    int EmployeeId,
    string Name,
    string Title,
    string Department,
    // girdiler
    decimal GrossSalary,
    int WorkedDays,
    decimal OvertimeHours,
    decimal PremiumPay,
    decimal RoadAllowance,
    decimal MealAllowance,
    decimal BenefitPay,
    decimal SpecialDeductions,
    decimal PreviousTaxBase,
    bool IbanComplete,
    bool TimesheetComplete,
    string ApprovalStatus,
    string? Notes,
    // motor çıktısı
    decimal HourlyRate,
    decimal OvertimePay,
    decimal BaseGross,
    decimal GrossEarnings,
    decimal SgkBase,
    decimal SgkEmployee,
    decimal UnemploymentEmployee,
    decimal IncomeTaxBase,
    decimal IncomeTax,
    decimal StampTax,
    decimal TotalDeductions,
    decimal NetPay,
    decimal EmployerSgk,
    decimal EmployerUnemployment,
    decimal EmployerCost,
    IReadOnlyList<string> Warnings);

public sealed record PayrollTotalsDto(decimal Gross, decimal Net, decimal EmployerCost, decimal Deductions);

/// <summary>Kontrol kartı — payroll.js controls[] şekli.</summary>
public sealed record PayrollControlDto(string Label, int Value, string Level);

public sealed record PayrollPeriodDetailDto(
    int Id,
    string Name,
    int Year,
    int Month,
    string Status,
    IReadOnlyList<PayrollRowDto> Rows,
    PayrollTotalsDto Totals,
    IReadOnlyList<PayrollControlDto> Controls);

/// <summary>Çalışanın kendi bordro pusulası listesi satırı.</summary>
public sealed record MyPayslipDto(
    int PeriodId,
    int RowId,
    string PeriodName,
    decimal GrossEarnings,
    decimal TotalDeductions,
    decimal NetPay,
    string ApprovalStatus);

public static class PayrollMappings
{
    /// <summary>Onay durumu — plan Ek A: Ön Hesap | Kontrol | Onaya Hazır | Eksik Veri | Onaylandı.</summary>
    public static string ToDto(this PayrollApprovalStatus status) => status switch
    {
        PayrollApprovalStatus.PreCalc => "Ön Hesap",
        PayrollApprovalStatus.Control => "Kontrol",
        PayrollApprovalStatus.ReadyForApproval => "Onaya Hazır",
        PayrollApprovalStatus.MissingData => "Eksik Veri",
        PayrollApprovalStatus.Approved => "Onaylandı",
        _ => status.ToString(),
    };

    public static string ToDto(this PayrollPeriodStatus status) => status switch
    {
        PayrollPeriodStatus.Draft => "draft",
        PayrollPeriodStatus.Control => "control",
        PayrollPeriodStatus.Approved => "approved",
        PayrollPeriodStatus.Closed => "closed",
        _ => status.ToString().ToLowerInvariant(),
    };

    private static readonly string[] TurkishMonths =
        ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran",
         "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

    /// <summary>Dönem adı: "Nisan 2026" (frontend period formatı).</summary>
    public static string PeriodName(int year, int month) => $"{TurkishMonths[month - 1]} {year}";

    /// <summary>PayrollSettings entity → motor parametreleri (dilimler Order sırasıyla).</summary>
    public static PayrollParameters ToEngineParameters(this PayrollSettings s) => new(
        s.OvertimeMultiplier,
        s.MonthlyWorkingHours,
        s.DefaultWorkedDays,
        s.SgkEmployeeRate,
        s.UnemploymentEmployeeRate,
        s.SgkEmployerRate,
        s.UnemploymentEmployerRate,
        s.StampTaxRate,
        s.SgkBaseMin,
        s.SgkBaseMax,
        s.MonthlyMinWageIncomeTaxExemption,
        s.MonthlyMinWageStampTaxExemption,
        s.TaxBrackets
            .OrderBy(b => b.Order)
            .Select(b => new PayrollBracket(b.Limit, b.Base, b.BaseTax, b.Rate))
            .ToList());

    /// <summary>PayrollEmployee girdisi → motor girdisi.</summary>
    public static PayrollInput ToEngineInput(this PayrollEmployee pe) => new(
        pe.GrossSalary,
        pe.WorkedDays,
        pe.OvertimeHours,
        pe.PremiumPay,
        pe.RoadAllowance,
        pe.MealAllowance,
        pe.BenefitPay,
        pe.SpecialDeductions,
        pe.PreviousTaxBase,
        pe.IbanComplete,
        pe.TimesheetComplete,
        AwaitingControl: pe.ApprovalStatus == PayrollApprovalStatus.Control);
}
