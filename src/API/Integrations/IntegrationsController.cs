using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Integrations;
using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Integrations;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class IntegrationsController : ControllerBase
{
    private readonly ILogger<IntegrationsController> _logger;
    private readonly IntegrationManager _integrationManager;

    public IntegrationsController(ILogger<IntegrationsController> logger, IntegrationManager integrationManager)
    {
        _logger = logger;
        _integrationManager = integrationManager;
    }

    [HttpGet("")]
    [ProducesResponseType<ListResponse<IntegrationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIntegrations()
    {
        Guid userId = User.GetUserId();

        var response = await _integrationManager.GetIntegrations(userId);

        return Ok(response);
    }

    [HttpGet("user")]
    [ProducesResponseType<ListResponse<UserIntegrationResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserIntegrations()
    {
        Guid userId = User.GetUserId();

        var response = await _integrationManager.GetUserIntegrations(userId);

        return Ok(response);
    }

    [HttpPost("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntegration([FromBody] CreateIntegrationRequest request)
    {
        Guid userId = User.GetUserId();

        /// FRONTEND SHOULD CALL ANOTHER ENDPOINT THAT STARTS THE OAUTH PROCESS
        /// THEN OAUTH REDIRECT SHOULD HAPPEN 
        /// OAUTH SHOULD THEN REDIRECT TO THIS ENDPOINT
        /// OAUTH REQUEST SHOULD BE ABLE TO HOLD USERS ID 
        /// THEN THIS ENDPOINT SHOULD RETURN A REDIRECT BACK TO INTEGRATION PAGE

        try
        {
            await _integrationManager.CreateIntegrationWithEncryption(userId, request.PlatformId, request.AccessToken);

            return Created();
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogWarning(ex, "No accessToken defined");
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request.",
                detail: "No AccessToken Defined"); ;
        }
    }

    [HttpDelete("{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteIntegration([FromRoute][Range(1, int.MaxValue)] int platformId)
    {
        Guid userId = User.GetUserId();

        await _integrationManager.DeleteIntegration(userId, platformId);

        return NoContent();
    }
}
