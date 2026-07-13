using IKPro.Application.Features.Payroll.Payslips;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace IKPro.Infrastructure.Pdf;

/// <summary>
/// QuestPDF ile bordro pusulası — payroll.js slip-paper önizlemesinin PDF karşılığı
/// (İK Pro başlık, kazanç/kesinti dökümü, net ödenecek).
/// </summary>
public sealed class QuestPdfPayslipGenerator : IPayslipGenerator
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    static QuestPdfPayslipGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Generate(PayslipModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Text("İK Pro").Bold().FontSize(18);
                        row.ConstantItem(220).AlignRight()
                            .Text($"{model.PeriodName} Bordro Pusulası").FontSize(11);
                    });
                    header.Item().PaddingTop(4).LineHorizontal(1);
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    content.Spacing(4);

                    content.Item().Text(text =>
                    {
                        text.Span("Personel: ").SemiBold();
                        text.Span($"{model.EmployeeName} — {model.Title} · {model.Department}");
                    });
                    content.Item().Text($"Çalışılan gün: {model.WorkedDays}   ·   " +
                        $"Fazla mesai: {model.OvertimeHours.ToString("0.##", Turkish)} saat");

                    content.Item().PaddingTop(10).Text("Kazançlar").Bold().FontSize(12);
                    content.Item().Element(e => MoneyRow(e, "Brüt ücret", model.BaseGross));
                    content.Item().Element(e => MoneyRow(e, "Fazla mesai", model.OvertimePay));
                    content.Item().Element(e => MoneyRow(e, "Prim", model.PremiumPay));
                    content.Item().Element(e => MoneyRow(e, "Yol yardımı", model.RoadAllowance));
                    content.Item().Element(e => MoneyRow(e, "Yemek yardımı", model.MealAllowance));
                    content.Item().Element(e => MoneyRow(e, "Yan hak / ek ödeme", model.BenefitPay));
                    content.Item().Element(e => MoneyRow(e, "Brüt kazanç", model.GrossEarnings, bold: true));

                    content.Item().PaddingTop(10).Text("Kesintiler").Bold().FontSize(12);
                    content.Item().Element(e => MoneyRow(e, "SGK işçi payı", model.SgkEmployee));
                    content.Item().Element(e => MoneyRow(e, "İşsizlik işçi payı", model.UnemploymentEmployee));
                    content.Item().Element(e => MoneyRow(e, "Gelir vergisi", model.IncomeTax));
                    content.Item().Element(e => MoneyRow(e, "Damga vergisi", model.StampTax));
                    content.Item().Element(e => MoneyRow(e, "Özel kesintiler", model.SpecialDeductions));
                    content.Item().Element(e => MoneyRow(e, "Toplam kesinti", model.TotalDeductions, bold: true));

                    content.Item().PaddingTop(14).BorderTop(1).PaddingTop(6)
                        .Element(e => MoneyRow(e, "NET ÖDENECEK", model.NetPay, bold: true, fontSize: 13));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("İK Pro · Bu pusula bilgilendirme amaçlıdır. ").FontSize(8);
                    text.Span(DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm 'UTC'", Turkish)).FontSize(8);
                });
            });
        }).GeneratePdf();
    }

    private static void MoneyRow(
        IContainer container, string label, decimal value, bool bold = false, int fontSize = 10)
    {
        container.Row(row =>
        {
            var money = value.ToString("C2", Turkish);
            row.RelativeItem().Text(label).FontSize(fontSize).SemiBold();
            var amount = row.ConstantItem(140).AlignRight().Text(money).FontSize(fontSize);
            if (bold) amount.Bold();
        });
    }
}
