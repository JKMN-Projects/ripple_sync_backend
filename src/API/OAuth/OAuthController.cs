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
    IntegrationManager integrationManager) : ControllerBase
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

        //URL of platform OAuth
        Uri? authorizationUrl = null;

        string state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        string codeVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        string codeChallenge;
        using (var sha256 = SHA256.Create())
        {
            byte[] challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            codeChallenge = Convert.ToBase64String(challengeBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        //Save userId and platformId in temp storage using new state generated
        // Storing
        OAuthStateData oauthData = new(User.GetUserId(), platformId, codeVerifier);

        await cache.SetAsync(
            $"oauth:{state}",
            oauthData,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            }, cancellationToken: cancellationToken);

        var secrets = configuration.GetSection("Integrations").GetSection(platform.ToString());

        string redirectUri = IsLocalEnvironment() ? "https://localhost:7275/api/oauth/callback" : "https://api.ripplesync.dk/api/oauth/callback";

        switch (platform)
        {
            //Provide state and more to OAuth.

            case Platform.X:
                string clientId = secrets["ClientId"]
                    ?? throw new InvalidOperationException("No ClientId found for X");

                QueryString queries = new QueryString()
                    .Add("response_type", "code")
                    .Add("client_id", clientId)
                    .Add("redirect_uri", redirectUri)
                    .Add("scope", "tweet.read+tweet.write+users.read+offline.access")
                    .Add("state", state)
                    .Add("code_challenge", codeChallenge)
                    .Add("code_challenge_method", "S256");

                authorizationUrl = new Uri("https://x.com/i/oauth2/authorize" + queries.ToUriComponent());
                break;
            case Platform.LinkedIn:
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

        if (authorizationUrl == null) return safeResult;

        //Frontend handles redirect, frontend can create a better redirect experience, with loading and such
        return Redirect(authorizationUrl.ToString());
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
            string redirectUri = IsLocalEnvironment() ? "https://localhost:7275/integrations" : "https://www.ripplesync.dk/integrations";

            switch (platform)
            {
                case Platform.X:
                    //string clientId = secrets["ClientId"]
                    //    ?? throw new InvalidOperationException("No ClientId found for X");

                    //string clientSecret = secrets["ClientSecret"]
                    //    ?? throw new InvalidOperationException("No ClientSecret found for X");

                    //QueryString queries = new QueryString()
                    //    .Add("grant_type", "authorization_code")
                    //    .Add("client_id", clientId)
                    //    .Add("client_secret", clientSecret)
                    //    .Add("redirect_uri", redirectUri)
                    //    .Add("code", code)
                    //    .Add("code_verifier", oauthData.CodeVerifier);

                    //accessTokenUrl = new Uri("https://api.x.com/2/oauth2/token" + queries.ToUriComponent());

                    string clientId = secrets["ClientId"]
                        ?? throw new InvalidOperationException("No ClientId found for X");

                    var formData = new Dictionary<string, string>
                    {
                        ["grant_type"] = "authorization_code",
                        ["client_id"] = clientId,
                        ["redirect_uri"] = redirectUri,
                        ["code"] = code,
                        ["code_verifier"] = oauthData.CodeVerifier
                    };

                    accessTokenUrl = new Uri("https://api.x.com/2/oauth2/token");
                    requestContent = new FormUrlEncodedContent(formData);
                    break;
                case Platform.LinkedIn:
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

            using var httpClient = new HttpClient();
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

            //Redirect back
            return Redirect("https://www.ripplesync.dk/integrations");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, ex.Message);
            return safeResult;
        }
    }

    private static bool IsLocalEnvironment()
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env == "Development" || env == "Local";
    }
}
