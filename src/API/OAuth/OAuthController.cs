using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using RippleSync.API.Common.Extensions;
using RippleSync.API.OAuth;
using RippleSync.Application.Integrations;
using RippleSync.Domain.Platforms;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RippleSync.API.Integrations;



[Route("api/[controller]")]
[Authorize]
[ApiController]
public partial class OAuthController : ControllerBase
{
    private readonly ILogger<OAuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly HybridCache _cache;
    private readonly IntegrationManager _integrationManager;

    public OAuthController(ILogger<OAuthController> logger, IConfiguration configuration, HybridCache cache, IntegrationManager integrationManager)
    {
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
        _integrationManager = integrationManager;
    }

    [HttpGet("initiate/{platformId:int}")]
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

        await _cache.SetAsync(
            $"oauth:{state}",
            oauthData,
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10)
            });

        var secrets = _configuration.GetSection("Integrations").GetSection(platform.ToString());

        string redirectUri = IsLocalEnvironment() ? "https://localhost:7275/api/oauth/callback" : "https://api.ripplesync.dk/api/oauth/callback";

        switch (platform)
        {
            //Provide state and more to OAuth.

            case Platforms.X:
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
            case Platforms.LinkedIn:
                break;
            case Platforms.Facebook:
                break;
            case Platforms.Instagram:
                break;
            case Platforms.Threads:
                break;
            default:
                return safeResult;
        }

        if (authorizationUrl == null) return safeResult;

        //Frontend handles redirect, frontend can create a better redirect experience, with loading and such
        return Ok(new { redirectUrl = authorizationUrl.ToString() });
    }

    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OAuthCallBack([FromQuery] string state, [FromQuery] string code)
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
            var oauthData = await _cache.GetOrCreateAsync(
                $"oauth:{state}",
                async cancel => (OAuthStateData?)null) // Returns null if not found
                    ?? throw new InvalidOperationException("Invalid or expired state");

            Platforms platform = (Platforms)oauthData.PlatformId;

            // Use state and code to get real token
            // Send get req
            Uri? accessTokenUrl = null;
            HttpContent? requestContent = null;

            var secrets = _configuration.GetSection("Integrations").GetSection(platform.ToString());
            string redirectUri = IsLocalEnvironment() ? "https://localhost:7275/integrations" : "https://www.ripplesync.dk/integrations";

            switch (platform)
            {
                case Platforms.X:
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
                case Platforms.LinkedIn:
                    break;
                case Platforms.Facebook:
                    break;
                case Platforms.Instagram:
                    break;
                case Platforms.Threads:
                    break;
                default:
                    return safeResult;
            }

            if (accessTokenUrl == null)
                return safeResult;

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(accessTokenUrl, requestContent);

            if (!response.IsSuccessStatusCode)
            {
                return Problem(
                    statusCode: (int)response.StatusCode,
                    title: "Token exchange failed",
                    detail: await response.Content.ReadAsStringAsync());
            }

            var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(
                await response.Content.ReadAsStreamAsync());

            if (tokenResponse == null)
            {
                return Problem("Failed to deserialize token response");
            }

            await _cache.RemoveAsync($"oauth:{state}");

            // Store
            await _integrationManager.CreateIntegrationWithEncryption(
                oauthData.UserId, oauthData.PlatformId,
                tokenResponse.AccessToken, tokenResponse.RefreshToken, tokenResponse.ExpiresIn, tokenResponse.TokenType, tokenResponse.Scope);

            //Redirect back
            return Redirect("https://www.ripplesync.dk/integrations");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, ex.Message);
            return safeResult;
        }
    }

    private static bool IsLocalEnvironment()
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return env == "Development" || env == "Local";
    }
}
