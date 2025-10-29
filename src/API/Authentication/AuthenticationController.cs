using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Users;

namespace RippleSync.API.Authentication;
[Route("api/[controller]")]
[ApiController]
public sealed class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UserManager _userManager;

    public AuthenticationController(ILogger<AuthenticationController> logger, UserManager userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPost("[action]")]
    [AllowAnonymous]
    [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        IActionResult safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request.",
                detail: "Invalid username or password.");

        try
        {
            AuthenticationTokenResponse tokenResponse = await _userManager.GetAuthenticationTokenAsync(request.Email, request.Password);
            HttpContext.Response.Cookies.Append("AccessToken", tokenResponse.Token, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
            return Ok(tokenResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed login attempt with email {Email}.", request.Email);
            return safeResult;
        }
        catch (EntityNotFoundException)
        {
            _logger.LogInformation("Failed login attempt with email {Email}. No user with that username.", request.Email);
            return safeResult;
        }
    }
}
