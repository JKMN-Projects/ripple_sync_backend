using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Users;
using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Integrations;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class OAuthController : ControllerBase
{
    private readonly ILogger<OAuthController> _logger;
    private readonly IntegrationManager _integrationManager;


    /// 
    /// POST /oauth/initiate - Backend generates state, stores userId mapping, returns X.com URL
    ///     Frontend redirects user to X.com
    /// X.com redirects to GET /oauth/callback? code = xxx & state = yyy
    ///     Backend validates state, retrieves userId, exchanges code for tokens with X.com
    ///         This is a new request to x.com to get real token
    ///     Backend stores tokens, redirects frontend to success page
    ///

    private enum Platforms
    {
        X = 1,
    }

    public OAuthController(ILogger<OAuthController> logger, IntegrationManager integrationManager)
    {
        _logger = logger;
        _integrationManager = integrationManager;
    }

    [HttpPost("initiate/{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateOauthForPlatform([FromRoute][Range(1, int.MaxValue)] int platformId)
    {
        IActionResult safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid platform.",
                detail: $"Platform ID {platformId} is not supported.");

        //Checks if platformId is supported
        if (!Enum.IsDefined(typeof(Platforms), platformId))
        {
            return safeResult;
        }

        Platforms platform = (Platforms)platformId;

        //URL of platform OAuth
        string authorizationUrl = string.Empty;

        //Save userId and platformId in temp storage using new state generated
        Guid userId = User.GetUserId();
        _ = platformId;

        switch (platform)
        {
            //Provide state and more to OAuth.

            case Platforms.X:
                // Generate OAuth URL logic here
                break;
            default:
                return safeResult;
        }

        //Frontend handles redirect, frontend can create a better redirect experience, with loading and such
        return Ok(new { authorizationUrl });
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> OAuthCallBack()
    {
        ///Browseren får aldig accesstoken at se, da OAuth 2.0 kræver at det er en server-server kommunikation, 
        /// hence vi får en intern kode at bruge til så at få token direkte fra os til dem og tilbage.
        try
        {
            // Get userId and platformId from temp storage using state recieved
            Guid userId = Guid.NewGuid();
            int platformId = 0;

            // Use state and code to get real token
            // Send get req
            string accessToken = string.Empty;

            // Store
            await _integrationManager.CreateIntegrationWithEncryption(userId, platformId, accessToken);

            //Redirect back
            return Redirect("https://www.ripplesync.dk/integrations");
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
}
