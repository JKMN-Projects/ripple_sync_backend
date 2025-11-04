using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Users;
using RippleSync.Application.Users.Exceptions;

namespace RippleSync.API.Authentication;
[Route("api/[controller]")]
[ApiController]
public sealed class AuthenticationController(
    ILogger<AuthenticationController> logger, 
    UserManager userManager) : ControllerBase
{
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
            AuthenticationTokenResponse tokenResponse = await userManager.GetAuthenticationTokenAsync(request.Email, request.Password, cancellationToken);
            SetAccessTokenCookie(tokenResponse.Token, tokenResponse.ExpiresAt);
            AuthenticationResponse response = new AuthenticationResponse(
                tokenResponse.RefreshToken,
                request.Email, 
                tokenResponse.ExpiresAt);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed login attempt with email {Email}.", request.Email);
            return safeResult;
        }
        catch (EntityNotFoundException)
        {
            logger.LogInformation("Failed login attempt with email {Email}. No user with that email.", request.Email);
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
            await userManager.RegisterUserAsync(request.Email, request.Password, cancellationToken);
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
            logger.LogInformation("Failed register attempt with email {Email}. Email already in use.", request.Email);
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
                    logger.LogWarning(ex, "Failed register attempt with email {Email}. Invalid email was supplied.", request.Email);
                    break;
                case "password":
                    logger.LogWarning(ex, "Failed register attempt with email {Email}. Invalid password was supplied.", request.Email);
                    break;
                default:
                    logger.LogWarning(ex, "Failed register attempt with email {Email}.", request.Email);
                    break;
            }
            return result;
        }
    }

    [HttpPost("[action]")]
    [AllowAnonymous]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request.",
                detail: "Invalid refresh token.");
        try
        {
            AuthenticationTokenResponse tokenResponse = await userManager.RefreshAuthenticationTokenAsync(request.RefreshToken);
            SetAccessTokenCookie(tokenResponse.Token, tokenResponse.ExpiresAt);
            string userEmail = tokenResponse.Claims.FindEmail()
                ?? throw new InvalidOperationException("Email claim not found.");
            AuthenticationResponse response = new AuthenticationResponse(
                tokenResponse.RefreshToken,
                userEmail,
                tokenResponse.ExpiresAt);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Failed token refresh attempt. Invalid refresh token was supplied.");
            ClearAccessTokenCookie();
            return safeResult;
        }
        catch (EntityNotFoundException)
        {
            logger.LogInformation("Failed token refresh attempt. No user found for the supplied refresh token.");
            return safeResult;
        }
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        ClearAccessTokenCookie();
        return NoContent();
    }


    [HttpDelete("user")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUser()
    {
        Guid userId = User.GetUserId();

        await userManager.DeleteUserAsync(userId);
        ClearAccessTokenCookie();

        return NoContent();
    }

    private void SetAccessTokenCookie(string token, long expiresAt)
    {
        HttpContext.Response.Cookies.Append("AccessToken", token, new CookieOptions
        {
            //Expires = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime,
            IsEssential = true,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
    }

    private void ClearAccessTokenCookie()
        => HttpContext.Response.Cookies.Delete("AccessToken");
}
