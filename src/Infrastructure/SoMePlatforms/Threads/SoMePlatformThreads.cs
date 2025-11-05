using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.SoMePlatforms.X;

namespace RippleSync.Infrastructure.SoMePlatforms.Threads;
internal class SoMePlatformThreads(IOptions<ThreadsOptions> options) : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", options.Value.AppId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "threads_manage_insights,threads_content_publish,threads_basic")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://threads.net/oauth/authorize" + queries.ToUriComponent()).ToString();
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

        return new HttpRequestMessage(HttpMethod.Post, "https://graph.threads.net/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(formData)
        };
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration)
    {
        return Task.FromResult(new PlatformStats(
            PostCount: 0,
            Reach: 0,
            Engagement: 0,
            Likes: 0
        ));
    }
    public Task<PostEvent> PublishPostAsync(Post post, Integration integration) => throw new NotImplementedException();
}
