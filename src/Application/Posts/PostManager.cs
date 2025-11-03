
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Diagnostics;

namespace RippleSync.Application.Posts;

public class PostManager(
    ILogger<PostManager> logger,
    IPostRepository postRepository,
    IPostQueries postQueries,
    IIntegrationRepository integrationRepository,
    IPlatformFactory platformFactory)
{
    public async Task<ListResponse<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
        => new(await postQueries.GetPostsByUserAsync(userId, status, cancellationToken));

    public async Task<TotalPostStatsResponse> GetPostStatForPeriodAsync(Guid userId, DateTime? from, CancellationToken cancellationToken = default)
    {
        IEnumerable<GetPostsByUserResponse> posts = await postQueries.GetPostsByUserAsync(userId, null, cancellationToken);
        IEnumerable<Integration> userIntegrations = await integrationRepository.GetByUserIdAsync(userId, cancellationToken);

        List<(string platformName, PlatformStats stats)> platformStats = [];

        foreach (var integration in userIntegrations)
        {
            IPlatform? platform = null;
            try
            {
                platform = platformFactory.Create(integration.Platform);
            }
            catch (ArgumentException argEx)
                when (argEx.ParamName == null || argEx.ParamName.Equals("platform", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Platform factory could not create platform for {Platform}. Skipping stats retrieval for this platform.", integration.Platform);
                continue;
            }
            PlatformStats stats = await platform.GetInsightsFromIntegrationAsync(integration);
            platformStats.Add((platformName: integration.Platform.ToString(), stats));
        }
        int publishedPosts = posts.Count(p => p.StatusName.Equals("Posted", StringComparison.OrdinalIgnoreCase));
        int scheduledPosts = posts.Count(p => p.StatusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase));

        return new TotalPostStatsResponse(
            PublishedPosts: publishedPosts,
            ScheduledPosts: scheduledPosts,
            TotalReach: platformStats.Sum(x => x.stats.Reach),
            TotalLikes: platformStats.Sum(x => x.stats.Likes),
            TotalStatsForPlatforms: platformStats.Select(ps => new TotalStatsForPlatform(
                Platform: ps.platformName,
                PublishedPosts: ps.stats.PostCount,
                Reach: ps.stats.Reach,
                Likes: ps.stats.Likes,
                AverageEngagement: ps.stats.Engagement > 0 && ps.stats.PostCount > 0
                    ? (double)ps.stats.Engagement / ps.stats.PostCount
                    : 0)).ToList()
        );
    }

    public async Task DeletePostByIdAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        //Request post first
        Post? post = await postRepository.GetByIdAsync(postId, cancellationToken);

        // Check if post belongs to user and if its deletable
        if (post == null || post.UserId != userId)
        {
            throw new UnauthorizedAccessException("Post does not belong to the user.");
        }
        if (post.IsDeletable() is false)
        {
            throw new InvalidOperationException("Post cannot be deleted in its current state.");
        }

        // Then delete
        await postRepository.DeleteAsync(post, cancellationToken);
    }
}
