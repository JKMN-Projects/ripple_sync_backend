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

    //[HttpPost("")]
    //[ProducesResponseType(StatusCodes.Status201Created)]
    //[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    //public async Task<IActionResult> CreateIntegration([FromBody] CreateIntegrationRequest request)
    //{
    //    Guid userId = User.GetUserId();

    //    /// FRONTEND SHOULD CALL ANOTHER ENDPOINT THAT STARTS THE OAUTH PROCESS
    //    /// THEN OAUTH REDIRECT SHOULD HAPPEN 
    //    /// OAUTH SHOULD THEN REDIRECT TO THIS ENDPOINT
    //    /// OAUTH REQUEST SHOULD BE ABLE TO HOLD USERS ID 
    //    /// THEN THIS ENDPOINT SHOULD RETURN A REDIRECT BACK TO INTEGRATION PAGE

    //    try
    //    {
    //        await _integrationManager.CreateIntegrationWithEncryption(userId, request.PlatformId, request.AccessToken);

    //        return Created();
    //    }
    //    catch (ArgumentNullException ex)
    //    {
    //        _logger.LogWarning(ex, "No accessToken defined");
    //        return Problem(
    //            statusCode: StatusCodes.Status400BadRequest,
    //            title: "Invalid request.",
    //            detail: "No AccessToken Defined"); ;
    //    }
    //}

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
