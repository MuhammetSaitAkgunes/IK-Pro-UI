using IKPro.Application.Common.Interfaces;
using IKPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Infrastructure.Identity;

/// <summary>Kiracının kullanıcı e-postalarını uygulama veritabanından okur.</summary>
public sealed class UserDirectorySource(AppDbContext context) : IUserDirectorySource
{
    public async Task<IReadOnlyList<string>> NormalizedEmailsAsync(
        int tenantId, CancellationToken cancellationToken) =>
        await context.Set<ApplicationUser>()
            .Where(u => u.TenantId == tenantId && u.NormalizedEmail != null)
            .Select(u => u.NormalizedEmail!)
            .ToListAsync(cancellationToken);
}
