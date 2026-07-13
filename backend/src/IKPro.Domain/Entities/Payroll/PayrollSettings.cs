using IKPro.Domain.Common;

namespace IKPro.Domain.Entities.Payroll;

/// <summary>
/// Bordro parametreleri (döneme/yıla göre versiyonlu). Değerler frontend
/// payrollDefaultSettings ile birebir. Oranlar yüzde/binde olarak saklanır.
/// </summary>
public class PayrollSettings : AuditableEntity
{
    /// <summary>Ayar setinin geçerli olduğu yıl (versiyonlama).</summary>
    public int Year { get; set; }
    public bool IsDefault { get; set; }

    public decimal OvertimeMultiplier { get; set; } = 1.5m;
    public decimal MonthlyWorkingHours { get; set; } = 225m;
    public int DefaultWorkedDays { get; set; } = 30;

    // Çalışan kesintileri (%)
    public decimal SgkEmployeeRate { get; set; } = 14m;
    public decimal UnemploymentEmployeeRate { get; set; } = 1m;

    // İşveren maliyetleri (%)
    public decimal SgkEmployerRate { get; set; } = 20.5m;
    public decimal UnemploymentEmployerRate { get; set; } = 2m;

    /// <summary>Damga vergisi oranı (binde).</summary>
    public decimal StampTaxRate { get; set; } = 0.759m;

    // SGK prime esas kazanç taban/tavan
    public decimal SgkBaseMin { get; set; } = 33030m;
    public decimal SgkBaseMax { get; set; } = 297270m;

    // Asgari ücret istisnaları (aylık)
    public decimal MonthlyMinWageIncomeTaxExemption { get; set; } = 4211m;
    public decimal MonthlyMinWageStampTaxExemption { get; set; } = 250.7m;

    /// <summary>Asgari ücret brütü (istisna/kontrol hesapları için).</summary>
    public decimal MinWageGross { get; set; } = 26005.5m;

    public ICollection<IncomeTaxBracket> TaxBrackets { get; set; } = new List<IncomeTaxBracket>();
}
