using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Platforms;

public interface IPlatform
{
    Task PublishPostAsync(Post post);
    Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration);
    //Task GetPostInsightsAsync(Post post);
}

public sealed record PlatformStats(
    int PostCount,
    int Reach,
    int Engagement,
    int Followers);