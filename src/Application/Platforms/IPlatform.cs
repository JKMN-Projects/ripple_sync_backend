using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Platforms;

public interface IPlatform
{
    Task<PostEvent> PublishPostAsync(Post post, Integration integration);
    Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration);
    //Task GetPostInsightsAsync(Post post);
}

public sealed record PlatformStats(
    int PostCount,
    int Reach,
    int Engagement,
    int Followers);

