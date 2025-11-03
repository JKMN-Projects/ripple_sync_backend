using Microsoft.AspNetCore.Http;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Text.Json;

namespace RippleSync.Infrastructure.SoMePlatforms;
internal class SoMePlatformFacebook : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", authConfig.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "read_insights,business_management,public_profile,pages_show_list,pages_manage_posts,pages_read_engagement")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://www.facebook.com/v24.0/dialog/oauth" + queries.ToUriComponent()).ToString();
    }

    public async Task<TokenResponse> GetTokenUrlAsync(TokenAccessConfiguration tokenConfigs, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = tokenConfigs.ClientId,
            ["client_secret"] = tokenConfigs.ClientSecret,
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code
        };

        var accessTokenUrl = new Uri("https://www.linkedin.com/oauth/v2/accessToken");
        var requestContent = new FormUrlEncodedContent(formData);

        var response = await httpClient.PostAsync(accessTokenUrl, requestContent, cancellationToken);

        return await JsonSerializer.DeserializeAsync<TokenResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Recieved token was null");
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration) => throw new NotImplementedException();
    public Task PublishPostAsync(Post post) => throw new NotImplementedException();
}
