using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Threading;

namespace RippleSync.Application.Platforms;

public interface ISoMePlatform
{
    string GetAuthorizationUrl(AuthorizationConfiguration authConfigs);
    HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs);
    Task PublishPostAsync(Post post);
    Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration);
    //Task GetPostInsightsAsync(Post post);
}