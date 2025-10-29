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
            HttpContext.Response.Cookies.Append("AccessToken", tokenResponse.Token);
            return Ok(tokenResponse);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Failed login attempt with email {Email}.", request.Email);
            return safeResult;
        }
        catch (EntityNotFoundException)
        {
            _logger.LogInformation("Failed login attempt with email {Email}. No user with that email.", request.Email);
            return safeResult;
        }
    }

    [HttpPost("[action]")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userManager.RegisterUserAsync(request.Email, request.Password, cancellationToken);
            return Created();
        }
        catch (ArgumentException ex)
        {
            Dictionary<string, string[]> validationErrors = [];
            if (ex.ParamName is not null)
            {
                validationErrors[ex.ParamName] = [ex.Message];
            }
            else
            {
                validationErrors["unknown"] = [ex.Message];
            }

            var result = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration data.",
                detail: ex.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "validationErrors", validationErrors }
                });
            switch (ex.ParamName)
            {
                case "email":
                    _logger.LogWarning(ex, "Failed register attempt with email {Email}. Invalid email was supplied.", request.Email);
                    break;
                case "password":
                    _logger.LogWarning(ex, "Failed register attempt with email {Email}. Invalid password was supplied.", request.Email);
                    break;
                default:
                    _logger.LogWarning(ex, "Failed register attempt with email {Email}.", request.Email);
                    break;
            }
            return result;
        }
        catch (InvalidOperationException ex)
        {
            var result = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration data.",
                detail: ex.Message);
            _logger.LogInformation(ex, "Failed register attempt with email {Email}. User already exists.", request.Email);
            return result;
        }
    }
}
