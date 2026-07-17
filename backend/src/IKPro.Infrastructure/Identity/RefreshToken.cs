namespace IKPro.Infrastructure.Identity;

/// <summary>
/// JWT yenileme token'ı. Erişim token'ı süresi dolunca yeni token almak için kullanılır.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>Sahip kiracı (token'ın ait olduğu kullanıcının kiracısı).</summary>
    public int TenantId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
