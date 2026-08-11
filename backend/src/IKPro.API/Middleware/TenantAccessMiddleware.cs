using IKPro.Application.Common.Interfaces;

namespace IKPro.API.Middleware;

/// <summary>
/// Erişim kapısının HTTP ayağı. <c>IdentityService.IssueTokensAsync</c> (login ve
/// refresh'in ortak yolu) yalnız token ÜRETİMİNİ korur; elinde kiracı
/// dondurulmadan ÖNCE alınmış hâlâ geçerli bir access token'ı olan istemci o
/// kontrolü hiç görmez. Bu middleware kimliği doğrulanmış HER istekte kapıyı
/// yeniden çalıştırarak o boşluğu kapatır.
///
/// JWT'deki <c>tenant</c> claim'ini okur (bkz. <see cref="IKPro.API.Services.CurrentTenant"/>
/// ile aynı claim adı). Claim yoksa istek DOKUNULMADAN geçer: anonim uçlar
/// (login/register/refresh/health), ve <c>X-Platform-Key</c> ile korunan
/// kiracı-üstü uçlar (TenancyController — provizyon/purge/rebuild-directory).
/// Bu uçlar kiracı bağlamı taşımaz; onları engellemek dondurulmuş bir kiracıyı
/// asla çözülemez hale getirirdi (geri yükleme prosedürü platform uçlarına bağlıdır).
///
/// Pipeline sırası önemli: <c>UseAuthentication()</c>'dan SONRA (claim'ler ancak
/// kimlik doğrulandıktan sonra okunabilir), <c>UseAuthorization()</c>'dan ÖNCE
/// (dondurulmuş kiracıyı rol kontrolünden önce keser — gereksiz yetki
/// değerlendirmesi çalıştırmaz).
/// </summary>
public sealed class TenantAccessMiddleware(RequestDelegate next)
{
    private const string TenantClaimType = "tenant";

    public async Task InvokeAsync(HttpContext context, ITenantAccessGuard accessGuard)
    {
        var tenantClaim = context.User.FindFirst(TenantClaimType)?.Value;
        if (tenantClaim is null || !int.TryParse(tenantClaim, out var tenantId))
        {
            await next(context);
            return;
        }

        // Bulunamazsa/erişilebilir değilse TenantInaccessibleException fırlatır —
        // GlobalExceptionHandler (UseExceptionHandler, bu middleware'den ÖNCE
        // pipeline'a eklendi) onu 403'e çevirir.
        await accessGuard.EnsureAccessibleAsync(tenantId, context.RequestAborted);

        await next(context);
    }
}
