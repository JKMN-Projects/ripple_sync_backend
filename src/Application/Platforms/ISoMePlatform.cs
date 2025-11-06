using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Platforms;

public interface ISoMePlatform
{
    string GetAuthorizationUrl(AuthorizationConfiguration authConfigs);
    HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs);
    Task<PostEvent> PublishPostAsync(Post post, Integration integration);
    Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform);
    //Task GetPostInsightsAsync(Post post);
}
