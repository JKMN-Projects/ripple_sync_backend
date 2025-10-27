using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Authentication;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Users;

namespace RippleSync.API.Integrations;

[Route("api/[controller]")]
[ApiController]
public class IntegrationsController : ControllerBase
{
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(ILogger<IntegrationsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIntegration([FromBody] CreateIntegrationRequest request)
    {
        return Ok();
        //IActionResult safeResult = Problem(
        //        statusCode: StatusCodes.Status400BadRequest,
        //        title: "Invalid request.",
        //        detail: "Invalid username or password.");

        //try
        //{

        //    return Ok();
        //}
        //catch (ArgumentException ex)
        //{
        //    _logger.LogWarning(ex, "Failed login attempt with email {Email}.", request.Email);
        //    return safeResult;
        //}
        //catch (EntityNotFoundException)
        //{
        //    _logger.LogInformation("Failed login attempt with email {Email}. No user with that username.", request.Email);
        //    return safeResult;
        //}
    }
}
