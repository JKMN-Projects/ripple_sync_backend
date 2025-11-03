using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.SoMePlatforms.X;
using System.Text.Json;

namespace RippleSync.Infrastructure.SoMePlatforms.Instagram;
internal class SoMePlatformInstagram(IOptions<InstagramOptions> options) : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", options.Value.AppId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "read_insights,business_management,pages_show_list,pages_manage_posts,pages_read_engagement,instagram_manage_insights,instagram_content_publish,pages_read_engagement")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://api.instagram.com/oauth/authorize" + queries.ToUriComponent()).ToString();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = options.Value.AppId,
            ["client_secret"] = options.Value.AppSecret,
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code,
            ["code_verifier"] = tokenConfigs.CodeVerifier
        };

        return new HttpRequestMessage(HttpMethod.Post, "https://api.instagram.com/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(formData)
        };
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration) => throw new NotImplementedException();
    public Task<PostEvent> PublishPostAsync(Post post, Integration integration) => throw new NotImplementedException();
}
