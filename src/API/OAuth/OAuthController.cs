using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using RippleSync.API.Common.Extensions;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Platforms;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RippleSync.API.OAuth;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class OAuthController(
    ILogger<OAuthController> logger,
    IConfiguration configuration,
    HybridCache cache,
    IntegrationManager integrationManager
    /*, OAuthManager oAuthManager*/) : ControllerBase
{

    [HttpGet("initiate/{platformId:int}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateOauthForPlatform([FromRoute][Range(1, int.MaxValue)] int platformId, CancellationToken cancellationToken = default)
    {
        IActionResult safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid platform.",
                detail: $"Platform ID {platformId} is not supported.");

        //Checks if platformId is supported
        if (!Enum.IsDefined(typeof(Platform), platformId))
        {
            return safeResult;
        }

        Platform platform = (Platform)platformId;

        

        //Frontend handles redirect, frontend can create a better redirect experience, with loading and such
        return Ok(new { redirectUrl = authorizationUrl.ToString() });
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthCallBack([FromQuery] string state, [FromQuery] string code, CancellationToken cancellationToken = default)
    {
        IActionResult safeResult = Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid platform.",
                detail: $"Platform is not supported.");

        ///Browseren får aldig accesstoken at se, da OAuth 2.0 kræver at det er en server-server kommunikation, 
        /// hence vi får en intern kode at bruge til så at få token direkte fra os til dem og tilbage.
        try
        {
            // Get userId and platformId from temp storage using state recieved
            var oauthData = await cache.GetOrCreateAsync(
                $"oauth:{state}",
                async cancel => (OAuthStateData?)null, cancellationToken: cancellationToken) // Returns null if not found
                    ?? throw new InvalidOperationException("Invalid or expired state");

            Platform platform = (Platform)oauthData.PlatformId;

            // Use state and code to get real token
            // Send get req
            Uri? accessTokenUrl = null;
            HttpContent? requestContent = null;

            var secrets = configuration.GetSection("Integrations").GetSection(platform.ToString());

            string redirectUri = configuration.GetSection("OAuth")["RedirectUrl"]
                ?? throw new InvalidOperationException("No Redirect found");

            string? credentials;
            string clientId;
            string clientSecret;
            Dictionary<string, string> formData;

            using var httpClient = new HttpClient();

            switch (platform)
            {
                case Platform.X:
                    clientId = secrets["ClientId"]
                        ?? throw new InvalidOperationException("No ClientId found for X");

                    clientSecret = secrets["ClientSecret"]
                        ?? throw new InvalidOperationException("No ClientSecret found for X");

                    formData = new Dictionary<string, string>
                    {
                        ["grant_type"] = "authorization_code",
                        ["redirect_uri"] = redirectUri,
                        ["code"] = code,
                        ["code_verifier"] = oauthData.CodeVerifier
                    };

                    accessTokenUrl = new Uri("https://api.x.com/2/oauth2/token");
                    requestContent = new FormUrlEncodedContent(formData);

                    // Add Basic Auth header
                    credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    break;
                case Platform.LinkedIn:
                    clientId = secrets["ClientId"]
                        ?? throw new InvalidOperationException("No ClientId found for LinkedIn");

                    clientSecret = secrets["ClientSecret"]
                        ?? throw new InvalidOperationException("No ClientSecret found for LinkedIn");

                    formData = new Dictionary<string, string>
                    {
                        ["grant_type"] = "authorization_code",
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["redirect_uri"] = redirectUri,
                        ["code"] = code
                    };

                    accessTokenUrl = new Uri("https://www.linkedin.com/oauth/v2/accessToken");
                    requestContent = new FormUrlEncodedContent(formData);
                    break;
                case Platform.Facebook:
                    break;
                case Platform.Instagram:
                    break;
                case Platform.Threads:
                    break;
                default:
                    return safeResult;
            }

            if (accessTokenUrl == null)
                return safeResult;

            var response = await httpClient.PostAsync(accessTokenUrl, requestContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Problem(
                    statusCode: (int)response.StatusCode,
                    title: "Token exchange failed",
                    detail: await response.Content.ReadAsStringAsync(cancellationToken));
            }

            var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            if (tokenResponse == null)
            {
                return Problem("Failed to deserialize token response");
            }

            await cache.RemoveAsync($"oauth:{state}", cancellationToken);

            // Store
            await integrationManager.CreateIntegrationWithEncryptionAsync(
                oauthData.UserId, oauthData.PlatformId,
                tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn, tokenResponse.TokenType, tokenResponse.Scope, cancellationToken);


            string redirectBackUri = configuration.GetSection("OAuth")["RedirectBackUrl"]
                ?? throw new InvalidOperationException("No Redirect found");

            //Redirect back
            return Redirect(redirectBackUri);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, ex.Message);
            return safeResult;
        }
    }
}
