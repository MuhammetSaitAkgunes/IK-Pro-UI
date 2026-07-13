using IKPro.Application.Features.Auth;
using IKPro.Application.Features.Auth.GetMe;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IKPro.API.Controllers;

/// <summary>Oturum açmış kullanıcının profili — apiClient.js sözleşmesi: GET /api/me.</summary>
[ApiController]
[Route("api/me")]
public sealed class MeController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Get(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetMeQuery(), cancellationToken));
}
