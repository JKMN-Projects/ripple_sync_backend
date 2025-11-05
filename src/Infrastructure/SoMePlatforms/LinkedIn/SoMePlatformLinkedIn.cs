using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.SoMePlatforms.X;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;
internal class SoMePlatformLinkedIn(IOptions<LinkedInOptions> options) : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", options.Value.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "w_member_social")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://www.linkedin.com/oauth/v2/authorization" + queries.ToUriComponent()).ToString();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = options.Value.ClientId,
            ["client_secret"] = options.Value.ClientSecret,
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code
        };

        return new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
        {
            Content = new FormUrlEncodedContent(formData)
        };
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration) => throw new NotImplementedException();
    public Task<PostEvent> PublishPostAsync(Post post, Integration integration) => throw new NotImplementedException();
}
