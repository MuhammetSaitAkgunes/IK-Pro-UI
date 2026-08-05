using FluentAssertions;
using IKPro.Domain.Services;

namespace IKPro.Tests.Unit.Payroll;

/// <summary>
/// ALTIN TEST — resmî rakamlara dayalı doğrulama.
///
/// PayrollEngineParityTests motoru eski arayüz prototipine sabitler; yani hesabın
/// DOĞRU olduğunu değil, mock ile AYNI olduğunu kanıtlar. Buradaki testler ise
/// kamuya açıklanmış resmî tutarları referans alır ve motorun mevzuata uygunluğunu
/// ölçer. Mali müşavir doğrulaması geldikçe (docs/bordro-mevzuat-dogrulama-talebi.md)
/// bu dosya genişletilecektir.
///
/// Kaynaklar (2026): brüt asgari ücret 33.030,00 TL, net asgari ücret 28.075,50 TL;
/// SGK PEK alt sınırı 33.030,00, üst sınırı 297.270,00 (5510 s.K. m.82 çarpanı
/// 01.01.2026'dan itibaren 7,5 → 9); ücret gelir vergisi tarifesi 190.000 / 400.000 /
/// 1.500.000 / 5.300.000 eşikleriyle %15-20-27-35-40.
/// </summary>
public sealed class PayrollResmiReferansTests
{
    private readonly PayrollEngine _engine = new();

    private const decimal MinWageGross2026 = 33030m;

    /// <summary>2026 resmî parametreleri (yürürlük: 01.01.2026).</summary>
    private static PayrollParameters Parameters2026 => new(
        OvertimeMultiplier: 1.5m,
        MonthlyWorkingHours: 225m,
        DefaultWorkedDays: 30,
        SgkEmployeeRate: 14m,
        UnemploymentEmployeeRate: 1m,
        SgkEmployerRate: 20.5m,
        UnemploymentEmployerRate: 2m,
        StampTaxRate: 0.759m,
        SgkBaseMin: MinWageGross2026,
        SgkBaseMax: 297270m,
        // İstisna = asgari ücret üzerinden hesaplanan gelir vergisi:
        // (33.030 − %15 SGK) × %15 = 28.075,50 × 0,15 = 4.211,325 → 4.211,33
        MonthlyMinWageIncomeTaxExemption: 4211.33m,
        MonthlyMinWageStampTaxExemption: 250.7m,
        TaxBrackets:
        [
            new PayrollBracket(190000m, 0m, 0m, 0.15m),
            new PayrollBracket(400000m, 190000m, 28500m, 0.20m),
            new PayrollBracket(1500000m, 400000m, 70500m, 0.27m),
            new PayrollBracket(5300000m, 1500000m, 367500m, 0.35m),
            new PayrollBracket(null, 5300000m, 1697500m, 0.40m),
        ]);

    /// <summary>
    /// Asgari ücretli, tam ay: net ücret resmî açıklanan 28.075,50 TL olmalı.
    /// Asgari ücret üzerinden gelir ve damga vergisi alınmaz (istisnalar tam karşılar).
    /// </summary>
    [Fact]
    public void AsgariUcretliTamAy_NetUcret_ResmiTutarlaAyni()
    {
        var input = new PayrollInput(MinWageGross2026, 30, 0, 0m, 0m, 0m, 0m, 0m, 0m);

        var result = _engine.Calculate(input, Parameters2026);

        result.SgkEmployee.Should().Be(4624.20m, "33.030 × %14");
        result.UnemploymentEmployee.Should().Be(330.30m, "33.030 × %1");
        result.IncomeTax.Should().Be(0m, "asgari ücret gelir vergisinden istisnadır");
        result.StampTax.Should().Be(0m, "asgari ücret damga vergisinden istisnadır");
        result.NetPay.Should().Be(28075.50m, "2026 resmî net asgari ücret");
    }

    /// <summary>SGK tavanı: 2026'da asgari ücretin 9 katı (5510 s.K. m.82).</summary>
    [Fact]
    public void TavaniAsanKazanc_SgkMatrahi_TavandaDurur()
    {
        var input = new PayrollInput(400000m, 30, 0, 0m, 0m, 0m, 0m, 0m, 0m);

        var result = _engine.Calculate(input, Parameters2026);

        result.SgkBase.Should().Be(297270m, "asgari ücret × 9");
        result.SgkEmployee.Should().Be(297270m * 0.14m);
    }
}
