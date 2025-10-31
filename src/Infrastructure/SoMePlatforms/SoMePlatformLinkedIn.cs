using Microsoft.AspNetCore.Http;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Text;
using System.Text.Json;

namespace RippleSync.Infrastructure.Platforms;
internal class SoMePlatformLinkedIn : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        QueryString queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", authConfig.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "w_member_social")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://www.linkedin.com/oauth/v2/authorization" + queries.ToUriComponent()).ToString();
    }

    public async Task<TokenResponse> GetTokenUrlAsync(TokenAccessConfiguration tokenConfigs, CancellationToken cancellationToken = default)
    {
        using HttpClient httpClient = new HttpClient();

        Dictionary<string, string> formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = tokenConfigs.ClientId,
            ["client_secret"] = tokenConfigs.ClientSecret,
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code
        };

        Uri accessTokenUrl = new Uri("https://www.linkedin.com/oauth/v2/accessToken");
        FormUrlEncodedContent requestContent = new FormUrlEncodedContent(formData);

        HttpResponseMessage response = await httpClient.PostAsync(accessTokenUrl, requestContent, cancellationToken);

        return await JsonSerializer.DeserializeAsync<TokenResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Recieved token was null");
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration) => throw new NotImplementedException();
    public Task PublishPostAsync(Post post) => throw new NotImplementedException();
}
