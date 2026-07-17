using Microsoft.AspNetCore.Identity;

namespace IKPro.Infrastructure.Identity;

/// <summary>
/// Kimlik doğrulama kullanıcısı. Bir <c>Employee</c> ile (EmployeeId) ilişkilendirilebilir.
/// Rol bilgisi Identity rolleri üzerinden yönetilir (hr-admin / manager / employee).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Sahip kiracı. Kullanıcı bir müşteri şirketine bağlıdır; login'de token'a gömülür.</summary>
    public int TenantId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? Initials { get; set; }

    /// <summary>Bağlı çalışan kaydı (Domain.Employee.Id).</summary>
    public int? EmployeeId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
