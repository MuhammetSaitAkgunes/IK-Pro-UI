using FluentAssertions;
using IKPro.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IKPro.Tests.Unit.Storage;

/// <summary>
/// Depo kökü sınırı testleri: kök dışına çıkan hiçbir yol okunmamalı/silinmemeli.
/// </summary>
public class LocalFileStorageTests : IDisposable
{
    private readonly string _base = Path.Combine(Path.GetTempPath(), $"ikpro-storage-{Guid.NewGuid():N}");
    private readonly string _root;
    private readonly string _sibling;

    public LocalFileStorageTests()
    {
        // Kritik kurulum: kardeş dizinin adı, kök adının ÖN EKİYLE başlıyor
        // ("store" ve "store-gizli"). Ayraçsız StartsWith karşılaştırması bu
        // kardeşi "kök içinde" sanır.
        _root = Path.Combine(_base, "store");
        _sibling = Path.Combine(_base, "store-gizli");
        Directory.CreateDirectory(_root);
        Directory.CreateDirectory(_sibling);
        File.WriteAllText(Path.Combine(_sibling, "sirlar.txt"), "baska kiracinin ozluk dosyasi");
    }

    private LocalFileStorage CreateStorage() =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:Root"] = _root })
            .Build());

    [Fact]
    public async Task OpenReadAsync_KomsuDizineSizanYol_Reddedilir()
    {
        var storage = CreateStorage();

        var escape = Path.Combine("..", "store-gizli", "sirlar.txt");

        await FluentActions
            .Invoking(() => storage.OpenReadAsync(escape, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_KomsuDizineSizanYol_DosyayiSilmez()
    {
        var storage = CreateStorage();
        var victim = Path.Combine(_sibling, "sirlar.txt");

        await FluentActions
            .Invoking(() => storage.DeleteAsync(Path.Combine("..", "store-gizli", "sirlar.txt"), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        File.Exists(victim).Should().BeTrue("kök dışındaki dosya silinmemeli");
    }

    [Fact]
    public async Task OpenReadAsync_KokIcindekiDosya_Okunur()
    {
        // Sınır kontrolü meşru kullanımı engellememeli (aşırı düzeltme koruması).
        var storage = CreateStorage();
        Directory.CreateDirectory(Path.Combine(_root, "belgeler"));
        await File.WriteAllTextAsync(Path.Combine(_root, "belgeler", "ozluk.txt"), "icerik");

        await using var stream = await storage.OpenReadAsync("belgeler/ozluk.txt", CancellationToken.None);
        using var reader = new StreamReader(stream);

        (await reader.ReadToEndAsync()).Should().Be("icerik");
    }

    public void Dispose()
    {
        if (Directory.Exists(_base)) Directory.Delete(_base, recursive: true);
        GC.SuppressFinalize(this);
    }
}
