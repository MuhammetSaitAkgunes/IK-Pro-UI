using FluentAssertions;
using IKPro.Domain.Common;
using IKPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IKPro.Tests.Unit.Persistence;

/// <summary>
/// Kiracı kapsamlı her tablo TenantId ile BAŞLAYAN bir indekse sahip olmalı.
///
/// Gerekçe: global query filter yüzünden uygulamadaki HER sorgu "TenantId = @p"
/// içerir. İndeks yoksa her sorgu tam tablo taraması yapar — tek bir uç noktayı
/// değil sistemin tamamını yavaşlatır ve ölçek büyüdükçe baskın maliyet olur.
///
/// Bu test kuralı modelde sabitler: yeni bir kiracı varlığı eklendiğinde indeks
/// otomatik gelmezse derleme değil ama test kırılır.
/// </summary>
public class TenantIndexTests
{
    private static AppDbContext ModelIcinContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            // Model kurmak için bağlantı açılmaz; dizenin geçerli olması yeterli.
            .UseSqlServer("Server=model-only;Database=model-only;Trusted_Connection=True;")
            .Options);

    [Fact]
    public void KiraciKapsamliHerTablo_TenantIdIleBaslayanIndekseSahiptir()
    {
        using var context = ModelIcinContext();

        var indekssiz = context.Model.GetEntityTypes()
            .Where(e => typeof(ITenantScoped).IsAssignableFrom(e.ClrType))
            // SQL view'larına indeks eklenemez; onlar altındaki tablolardan beslenir.
            .Where(e => e.GetViewName() is null)
            .Where(e => !e.GetIndexes().Any(i =>
                i.Properties.Count > 0 && i.Properties[0].Name == nameof(ITenantScoped.TenantId)))
            .Select(e => e.ClrType.Name)
            .OrderBy(name => name)
            .ToArray();

        indekssiz.Should().BeEmpty(
            "TenantId ile başlayan indeksi olmayan tablolar var: {0}", string.Join(", ", indekssiz));
    }
}
