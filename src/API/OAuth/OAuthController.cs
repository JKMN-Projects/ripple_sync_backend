using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.OAuth;
using RippleSync.Domain.Platforms;
using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.OAuth;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class OAuthController(
    ILogger<OAuthController> logger,
    IConfiguration configuration,
    OAuthManager oauthManager) : ControllerBase
{

    [HttpGet("initiate/{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateOauthForPlatform([FromRoute][Range(1, int.MaxValue)] int platformId, CancellationToken cancellationToken = default)
    {
        try
        {
            //Checks if platformId is supported
            if (!Enum.IsDefined(typeof(Platform), platformId))
            {
                return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid platform.",
                    detail: $"Platform ID {platformId} is not supported."); ; ;
            }

            var authorizationUrl = await oauthManager.GetAuthorizationUrl(User.GetUserId(), (Platform)platformId, cancellationToken);

            //Frontend handles redirect, frontend can create a better redirect experience, with loading and such
            return Ok(new { redirectUrl = authorizationUrl });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error on initiate");
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                    title: "Error on initiate",
                    detail: ex.Message);
        }
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthCallBack([FromQuery] string state, [FromQuery] string code, CancellationToken cancellationToken = default)
    {
        ///Browseren får aldig accesstoken at se, da OAuth 2.0 kræver at det er en server-server kommunikation, 
        /// hence vi får en intern kode at bruge til så at få token direkte fra os til dem og tilbage.
        try
        {
            await oauthManager.StoreToken(state, code, cancellationToken);

            string redirectBackUri = configuration.GetSection("OAuth")["RedirectBackUrl"]
                ?? throw new InvalidOperationException("No Redirect found");

            //Redirect back
            return Redirect(redirectBackUri);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Error on Callback");
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                    title: "Error on Callback",
                    detail: ex.Message);
        }
    }
}
