using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Threading;

namespace RippleSync.Application.Platforms;

public interface ISoMePlatform
{
    string GetAuthorizationUrl(AuthorizationConfiguration authConfigs);
    Task<TokenResponse> GetTokenUrlAsync(TokenAccessConfiguration tokenConfigs, CancellationToken cancellationToken);
    Task PublishPostAsync(Post post);
    Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration);
    //Task GetPostInsightsAsync(Post post);
}

public record AuthorizationConfiguration(
    string ClientId,
    string RedirectUri,
    string State,
    string CodeChallenge
);

public record TokenAccessConfiguration(
    string ClientId,
    string ClientSecret,
    string RedirectUri,
    string Code,
    string CodeVerifier
);

public sealed record PlatformStats(
    int PostCount,
    int Reach,
    int Engagement,
    int Followers);