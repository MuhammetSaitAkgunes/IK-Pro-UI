using FluentAssertions;
using IKPro.Application.Common.Interfaces;
using IKPro.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;

namespace IKPro.Tests.Integration.Email;

/// <summary>
/// E-posta göndericisi seçimi: Email:Mode yapılandırılmadığında (test/dev varsayılanı)
/// dosya outbox stub'ı kullanılmalı — davet testleri token'ı outbox'tan okur.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class EmailSenderSelectionTests(IKProApiFactory factory)
{
    [Fact]
    public void DefaultMode_UsesFileOutbox()
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .Should().BeOfType<FileOutboxEmailSender>("Email:Mode yapılandırılmadığında outbox varsayılandır");
    }
}
