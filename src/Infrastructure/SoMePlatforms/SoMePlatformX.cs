using Microsoft.AspNetCore.Http;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Text;
using System.Text.Json;

namespace RippleSync.Infrastructure.SoMePlatforms;
internal class SoMePlatformX : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", authConfig.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "tweet.read+tweet.write+users.read+offline.access")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://x.com/i/oauth2/authorize" + queries.ToUriComponent()).ToString();
    }

    public async Task<TokenResponse> GetTokenUrlAsync(TokenAccessConfiguration tokenConfigs, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.CodeVerifier,
            ["code_verifier"] = tokenConfigs.CodeVerifier
        };

        var accessTokenUrl = new Uri("https://api.x.com/2/oauth2/token");
        var requestContent = new FormUrlEncodedContent(formData);

        // Basic Auth header
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{tokenConfigs.ClientId}:{tokenConfigs.ClientSecret}"));

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        var response = await httpClient.PostAsync(accessTokenUrl, requestContent, cancellationToken);

        return await JsonSerializer.DeserializeAsync<TokenResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Recieved token was null");
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration) => throw new NotImplementedException();
    public Task PublishPostAsync(Post post) => throw new NotImplementedException();
}
