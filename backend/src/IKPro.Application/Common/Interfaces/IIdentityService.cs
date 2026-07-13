using IKPro.Application.Features.Auth;

namespace IKPro.Application.Common.Interfaces;

/// <summary>
/// Kimlik işlemleri soyutlaması. Infrastructure'da ASP.NET Core Identity + JWT ile
/// implemente edilir; Application katmanı Identity paketlerine bağımlı kalmaz.
/// </summary>
public interface IIdentityService
{
    /// <summary>E-posta + şifre ile giriş; başarısızsa <c>UnauthorizedException</c>.</summary>
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Yeni kullanıcı kaydı; e-posta çakışmasında <c>ConflictException</c>.</summary>
    Task<AuthResponse> RegisterAsync(string name, string email, string password, string role, CancellationToken cancellationToken);

    /// <summary>Refresh token rotasyonu: eskisini iptal eder, yeni çift üretir.</summary>
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>Refresh token'ı iptal eder (çıkış).</summary>
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken);
}
