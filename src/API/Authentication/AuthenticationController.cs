using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Users;
using RippleSync.Application.Users.Exceptions;

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
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        IActionResult safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request.",
                detail: "Invalid username or password.");

        try
        {
            AuthenticationTokenResponse tokenResponse = await _userManager.GetAuthenticationTokenAsync(request.Email, request.Password, cancellationToken);
            HttpContext.Response.Cookies.Append("AccessToken", tokenResponse.Token, new CookieOptions
            {
                //Expires = DateTimeOffset.FromUnixTimeMilliseconds(tokenResponse.ExpiresAt).UtcDateTime,
                IsEssential = true,
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

            AuthenticationResponse response = new AuthenticationResponse(request.Email, tokenResponse.ExpiresAt);

            return Ok(response);
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
        catch (EmailAlreadyInUseException ex)
        {
            var result = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Email already in use.",
                detail: ex.Message,
                extensions: new Dictionary<string, object?>
                {
                    { "validationErrors", new Dictionary<string, string[]>
                        {
                            { "email", [ex.Message, "TEST ERROR"] }
                        }
                    }
                });
            _logger.LogInformation("Failed register attempt with email {Email}. Email already in use.", request.Email);
            return result;
        }
        catch (ArgumentException ex)
        {
            Dictionary<string, string[]> validationErrors = [];
            string trimmedMessage = ex.Message.Replace($"(Parameter '{ex.ParamName}')", "").Trim();
            if (ex.ParamName is not null)
            {
                validationErrors[ex.ParamName.ToLowerInvariant()] = [trimmedMessage];
            }
            else
            {
                validationErrors["unknown"] = [trimmedMessage];
            }

            var result = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid registration data.",
                detail: ex.Message.Replace($"(Parameter '{ex.ParamName}')", "").Trim(),
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
    }

    [HttpDelete("user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser()
    {
        Guid userId = User.GetUserId();

        await _userManager.DeleteUserAsync(userId);

        return NoContent();
    }
}
