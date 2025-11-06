using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Integrations;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Platforms;
using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Integrations;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class IntegrationsController(ILogger<IntegrationsController> logger, IntegrationManager integrationManager) : ControllerBase
{
    [HttpGet("")]
    [ProducesResponseType<ListResponse<PlatformWithUserIntegrationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatformsWithUserIntegrationsAsync()
    {
        Guid userId = User.GetUserId();

        var response = await integrationManager.GetPlatformsWithUserIntegrationsAsync(userId);

        return Ok(response);
    }

    [HttpGet("user")]
    [ProducesResponseType<ListResponse<ConnectedIntegrationsResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserIntegrations()
    {
        Guid userId = User.GetUserId();

        var response = await integrationManager.GetConnectedIntegrationsAsync(userId);

        return Ok(response);
    }

    [HttpDelete("{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteIntegration([FromRoute][Range(1, int.MaxValue)] int platformId)
    {
        Guid userId = User.GetUserId();
        if (!Enum.IsDefined(typeof(Platform), platformId))
        {
            logger.LogWarning("Invalid platform ID: {PlatformId}", platformId);
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request.",
                detail: "Invalid Platform ID");
        }

        await integrationManager.DeleteIntegrationAsync(userId, (Platform)platformId);

        return NoContent();
    }
}
