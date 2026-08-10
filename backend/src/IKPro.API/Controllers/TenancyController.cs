using IKPro.Application.Features.Tenancy.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace IKPro.API.Controllers;

/// <summary>
/// Platform (kiracı-üstü) uçları. Kiracı provizyonu normal rol sisteminin DIŞINDADIR —
/// bir kiracı başka bir kiracı açamaz. Bu yüzden JWT değil, sunucu-tarafı bir
/// <c>X-Platform-Key</c> ile korunur (yapılandırmadan/ortamdan gelir).
/// Faz 1: yalnız provizyon; ileride self-servis kayıt ayrı ve açık bir akış olacak.
/// </summary>
[ApiController]
[Route("api/tenants")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public sealed class TenancyController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private const string PlatformKeyHeader = "X-Platform-Key";

    [HttpPost]
    [ProducesResponseType<ProvisionTenantResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProvisionTenantResult>> Provision(
        ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        if (!PlatformKeyValid())
        {
            return Unauthorized(new { title = "Platform anahtarı geçersiz veya eksik." });
        }

        var result = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <remarks>Bir kiracının TÜM verisini kalıcı siler (KVKK). Platform-key + confirmSlug zorunlu.</remarks>
    [HttpDelete("{id:int}")]
    [ProducesResponseType<PurgeTenantResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurgeTenantResult>> Purge(
        int id, [FromQuery] string? confirmSlug, CancellationToken cancellationToken)
    {
        if (!PlatformKeyValid())
        {
            return Unauthorized(new { title = "Platform anahtarı geçersiz veya eksik." });
        }

        return Ok(await sender.Send(new PurgeTenantCommand(id, confirmSlug ?? ""), cancellationToken));
    }

    /// <remarks>Doğrulanmamış (pasif, davet kabul edilmemiş) eski kiracıları toplu siler. Cron.</remarks>
    [HttpPost("cleanup-unverified")]
    [ProducesResponseType<CleanupUnverifiedResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CleanupUnverifiedResult>> CleanupUnverified(
        [FromQuery] int olderThanDays, CancellationToken cancellationToken)
    {
        if (!PlatformKeyValid())
        {
            return Unauthorized(new { title = "Platform anahtarı geçersiz veya eksik." });
        }

        return Ok(await sender.Send(new CleanupUnverifiedTenantsCommand(olderThanDays), cancellationToken));
    }

    /// <remarks>Kiracının yönlendirme dizinini yeniden kurar (geri yükleme sonrası zorunlu adım).</remarks>
    [HttpPost("{id:int}/rebuild-directory")]
    [ProducesResponseType<RebuildDirectoryResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RebuildDirectoryResult>> RebuildDirectory(
        int id, CancellationToken cancellationToken)
    {
        if (!PlatformKeyValid())
        {
            return Unauthorized(new { title = "Platform anahtarı geçersiz veya eksik." });
        }

        return Ok(await sender.Send(new RebuildDirectoryCommand(id), cancellationToken));
    }

    private bool PlatformKeyValid()
    {
        var expected = configuration["Platform:ProvisioningKey"];
        var provided = Request.Headers[PlatformKeyHeader].ToString();
        return !string.IsNullOrEmpty(expected) && provided == expected;
    }

    /// <remarks>Self-servis kayıt (public, platform anahtarı yok, 'signup' rate-limit'li).
    /// Kiracı pasif oluşturulur; admin davet e-postasını kabul edince etkinleşir.</remarks>
    [HttpPost("signup")]
    [EnableRateLimiting("signup")]
    [ProducesResponseType<RegisterTenantResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterTenantResult>> Signup(
        RegisterTenantCommand command, CancellationToken cancellationToken)
        => StatusCode(StatusCodes.Status201Created, await sender.Send(command, cancellationToken));
}
