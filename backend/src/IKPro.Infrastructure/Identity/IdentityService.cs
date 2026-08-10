using FluentValidation;
using FluentValidation.Results;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Features.Auth;
using IKPro.Domain.Constants;
using IKPro.Domain.Entities.Tenancy;
using IKPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace IKPro.Infrastructure.Identity;

/// <summary>
/// <see cref="IIdentityService"/> implementasyonu: ASP.NET Core Identity + JWT.
/// Lockout SignInManager üzerinden işler; refresh token'lar rotasyonla yenilenir
/// ve DB'de hash'lenmiş saklanır.
/// </summary>
public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    JwtTokenService tokenService,
    ICurrentTenant currentTenant,
    IEmailSender emailSender,
    IConfiguration configuration,
    AppDbContext context,
    IPlatformDbContext platform) : IIdentityService
{
    private const string InvalidCredentialsMessage = "E-posta veya şifre hatalı.";

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Hesap pasif durumda. Yöneticinizle iletişime geçin.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            throw new UnauthorizedException("Çok sayıda hatalı deneme; hesap geçici olarak kilitlendi.");
        }

        if (!result.Succeeded)
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Multi-tenant: kullanıcının kiracısı (şirketi) askıya alınmışsa girişe izin verilmez.
        // Kiracı kimliği platform veritabanındadır.
        var tenant = await platform.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId, cancellationToken);
        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            throw new UnauthorizedException("Şirket hesabı aktif değil. Yöneticinizle iletişime geçin.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RegisterAsync(
        string name, string email, string password, string role, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = name,
            Initials = DeriveInitials(name),
            // Faz 0/1: anonim self-servis kayıt henüz kiracı bağlamı taşımaz → aktif
            // kiracı yoksa varsayılan (ilk) kiracıya bağlanır. Faz 1 (self-servis) bunu
            // gerçek kiracı seçimi/davetiyle değiştirecek.
            TenantId = currentTenant.TenantId ?? await DefaultTenantIdAsync(cancellationToken),
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            // Identity parola/kullanıcı politikası ihlalleri 400 doğrulama hatası olarak döner.
            throw new ValidationException(createResult.Errors
                .Select(e => new ValidationFailure("password", e.Description)));
        }

        await DizineYazAsync(email, user.TenantId, cancellationToken);
        await userManager.AddToRoleAsync(user, role);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task ReserveEmailAsync(string email, int tenantId, CancellationToken cancellationToken)
        => await DizineYazAsync(email, tenantId, cancellationToken);

    public async Task CreateTenantAdminAsync(
        int tenantId, string name, string email, string companyName, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = name,
            Initials = DeriveInitials(name),
            TenantId = tenantId,
        };

        // Dizine CreateInvitedUserAsync KOŞULSUZ yazar (idempotent DizineYazAsync).
        // Çağıran burada ReserveEmailAsync ile önceden rezervasyon yapmış olabilir
        // (TenantOnboarding) — o durumda bu ikinci yazım aynı kiracı için sessizce
        // no-op'tur. Rezervasyon yapılmamışsa (ör. eski bir çağıran unutursa) bu
        // çağrı yine de dizine yazar; "dizine hiç yazılmadı" sınıfı hatalar artık
        // derleyici tarafından değil, tek bir kod yolu tarafından imkansız kılınır.
        await CreateInvitedUserAsync(user, Roles.HrAdmin, companyName, cancellationToken);
    }

    public async Task CreateEmployeeLoginAsync(
        int employeeId, string name, string email, CancellationToken cancellationToken)
    {
        // İşe alım her zaman kimliği doğrulanmış bir hr-admin tarafından yapılır →
        // yeni personel login'i o admin'in kiracısına bağlanır.
        var tenantId = currentTenant.TenantIdOrThrow();
        var companyName = await TenantNameAsync(tenantId, cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = name,
            Initials = DeriveInitials(name),
            EmployeeId = employeeId,
            TenantId = tenantId,
        };
        await CreateInvitedUserAsync(user, Roles.Employee, companyName, cancellationToken);
    }

    public async Task AcceptInviteAsync(
        string email, string token, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new ValidationException([new ValidationFailure("email", "Davet bulunamadı.")]);

        // Şifresiz oluşturulan hesabın ilk şifresini davet (reset) token'ıyla belirler.
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors
                .Select(e => new ValidationFailure("token", e.Description)));
        }

        // Şifre belirlendi = e-posta doğrulandı. Self-servis kayıtta Provisioning
        // durumunda oluşturulan kiracıyı ilk admin kabulünde etkinleştir.
        var tenant = await platform.Tenants.FirstOrDefaultAsync(
            t => t.Id == user.TenantId, cancellationToken);
        if (tenant is { Status: TenantStatus.Provisioning })
        {
            tenant.Status = TenantStatus.Active;
            await platform.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Kullanıcıyı ŞİFRESİZ oluşturur, rol atar ve şifre-belirleme (davet) token'ını
    /// e-postayla gönderir. Kullanıcı <c>accept-invite</c> ile hesabını etkinleştirir.
    ///
    /// Kullanıcı oluşturma başarılı olur olmaz KOŞULSUZ dizine yazar (bkz.
    /// <see cref="DizineYazAsync"/> — idempotenttir, çağıran önceden rezervasyon
    /// yapmışsa aynı kiracı için sessizce no-op'tur).
    /// </summary>
    private async Task CreateInvitedUserAsync(
        ApplicationUser user, string role, string companyName, CancellationToken cancellationToken)
    {
        if (await userManager.FindByEmailAsync(user.Email!) is not null)
        {
            throw new ConflictException($"'{user.Email}' e-postasıyla kayıtlı bir hesap zaten var.");
        }

        var createResult = await userManager.CreateAsync(user); // şifresiz → davet gerektirir
        if (!createResult.Succeeded)
        {
            throw new ValidationException(createResult.Errors
                .Select(e => new ValidationFailure("email", e.Description)));
        }

        await DizineYazAsync(user.Email!, user.TenantId, cancellationToken);

        await userManager.AddToRoleAsync(user, role);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await SendInviteEmailAsync(user.DisplayName, user.Email!, companyName, token, cancellationToken);
    }

    /// <summary>
    /// Kullanıcıyı yönlendirme dizinine İDEMPOTENT yazar ve ÇAKIŞMAYI 409'a çevirir.
    ///
    /// Dizinin birincil anahtarı e-postadır; "tek e-posta = tek kiracı" kuralı
    /// burada, veritabanı seviyesinde uygulanır:
    ///   - kayıt yoksa → eklenir,
    ///   - kayıt var ve AYNI kiracıya aitse → sessizce geçilir (no-op),
    ///   - kayıt var ve BAŞKA kiracıya aitse → <c>ConflictException</c>.
    ///
    /// İdempotent olması, çağrının GÜVENLE tekrarlanabilmesini sağlar: hem önceden
    /// rezervasyon yapmış bir çağıranın (ör. <see cref="ReserveEmailAsync"/>) ardından
    /// kullanıcı oluşturma yolunun tekrar çağırması sorun çıkarmaz, hem de rezervasyonu
    /// atlayan bir çağıran için dizine yazma güvenlik ağı olarak kalır — böylece
    /// "dizine hiç yazılmadı" sınıfı hatalar tek bir sözleşmeye (çağıranın önceden
    /// rezerve ettiğini varsaymak) bağımlı olmaktan çıkar.
    ///
    /// Yukarıdaki okuma-sonra-karar mantığı eşzamanlı iki isteği ayıramaz (TOCTOU);
    /// asıl güvence alttaki <c>catch</c>'tir — INSERT birincil anahtara çarparsa da
    /// 409'a çevrilir.
    ///
    /// Dizine yazan TEK YER burası DEĞİLDİR: <c>RebuildDirectoryCommand</c> de
    /// <c>platform.Directory.Add</c> çağırır. İkisinin semantiği kasıtlı olarak
    /// FARKLIDIR — burası kullanıcı OLUŞTURURKEN çakışmayı 409'a çevirip reddeder
    /// (yetkisiz bir yazının başka kiracıyı ele geçirmesini önler), yeniden kurma
    /// ise zaten yetkili kaynaktan (kiracının kendi Users tablosu) yazdığı için
    /// çakışan satırları reddetmek yerine atlar ve raporlar.
    /// </summary>
    private async Task DizineYazAsync(string email, int tenantId, CancellationToken cancellationToken)
    {
        var normalizedEmail = TenantDirectoryEntry.Normalize(email);

        var mevcut = await platform.Directory
            .FirstOrDefaultAsync(d => d.NormalizedEmail == normalizedEmail, cancellationToken);

        if (mevcut is not null)
        {
            if (mevcut.TenantId != tenantId)
            {
                throw new ConflictException($"'{email}' e-postasıyla kayıtlı bir hesap zaten var.");
            }

            return; // Aynı kiracı için zaten rezerve/yazılmış — idempotent no-op.
        }

        platform.Directory.Add(new TenantDirectoryEntry
        {
            NormalizedEmail = normalizedEmail,
            TenantId = tenantId,
        });

        try
        {
            await platform.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Eşzamanlı bir istek yukarıdaki kontrolden SONRA, biz SaveChanges'ten ÖNCE
            // aynı e-postayı kaptı: TOCTOU. INSERT birincil anahtara çarptı.
            throw new ConflictException($"'{email}' e-postasıyla kayıtlı bir hesap zaten var.");
        }
    }

    private async Task SendInviteEmailAsync(
        string name, string email, string companyName, string token, CancellationToken cancellationToken)
    {
        var appUrl = (configuration["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var link = $"{appUrl}/#/accept-invite?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        var body =
            $"Merhaba {name},\n\n{companyName} için İK Pro hesabınız oluşturuldu. " +
            $"Şifrenizi belirleyip hesabınızı etkinleştirmek için:\n{link}\n\n" +
            $"Bağlantı çalışmazsa e-postanız ve şu davet kodunu kullanın:\nDAVET-KODU: {token}\n";
        await emailSender.SendAsync(new EmailMessage(email, "İK Pro hesap daveti", body), cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = JwtTokenService.Hash(refreshToken);
        var stored = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == hash, cancellationToken);

        if (stored is null || !stored.IsActive || stored.User is null || !stored.User.IsActive)
        {
            throw new UnauthorizedException("Geçersiz veya süresi dolmuş yenileme token'ı.");
        }

        // Rotasyon: eski token tek kullanımlıktır.
        stored.RevokedAtUtc = DateTime.UtcNow;

        return await IssueTokensAsync(stored.User, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = JwtTokenService.Hash(refreshToken);
        var stored = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == hash, cancellationToken);

        // Idempotent: token bulunamazsa sessizce başarı (zaten çıkış yapılmış).
        if (stored is not null && stored.RevokedAtUtc is null)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedException("Kullanıcı kaydı bulunamadı.");

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return;
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            throw new UnauthorizedException("Mevcut şifre hatalı.");
        }

        throw new ValidationException(result.Errors
            .Select(e => new ValidationFailure("newPassword", e.Description)));
    }

    public async Task<UserDto?> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : await BuildUserDtoAsync(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresAtUtc) = tokenService.CreateAccessToken(user, roles);
        var (rawRefreshToken, refreshEntity) = tokenService.CreateRefreshToken(user.Id);
        refreshEntity.TenantId = user.TenantId;

        context.RefreshTokens.Add(refreshEntity);
        await context.SaveChangesAsync(cancellationToken);

        var tenantName = await TenantNameAsync(user.TenantId, cancellationToken);
        return new AuthResponse(accessToken, rawRefreshToken, expiresAtUtc, ToUserDto(user, roles, tenantName));
    }

    private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user)
        => ToUserDto(user, await userManager.GetRolesAsync(user), await TenantNameAsync(user.TenantId, default));

    /// <summary>Kiracının görünen adı — /me ve auth yanıtlarında şirket bağlamı için.</summary>
    private async Task<string> TenantNameAsync(int tenantId, CancellationToken cancellationToken) =>
        await platform.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

    private static UserDto ToUserDto(ApplicationUser user, IEnumerable<string> roles, string tenantName)
    {
        var role = roles.FirstOrDefault() ?? Roles.Employee;

        return new UserDto(
            user.Id,
            user.DisplayName,
            user.Email ?? string.Empty,
            role,
            Roles.LabelOf(role),
            user.Initials ?? DeriveInitials(user.DisplayName),
            user.EmployeeId,
            user.TenantId,
            tenantName);
    }

    /// <summary>Varsayılan (ilk) kiracı — anonim kayıtta kullanılır (Faz 1'de değişecek).</summary>
    private async Task<int> DefaultTenantIdAsync(CancellationToken cancellationToken) =>
        await platform.Tenants
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .FirstAsync(cancellationToken);

    /// <summary>Ad soyaddan baş harfler, ör. "Ahmet Yılmaz" → "AY" (frontend initials paritesi).</summary>
    private static string DeriveInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpper(new System.Globalization.CultureInfo("tr-TR")),
            _ => string.Concat(parts[0][..1], parts[^1][..1]).ToUpper(new System.Globalization.CultureInfo("tr-TR")),
        };
    }
}
