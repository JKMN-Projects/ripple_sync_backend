
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

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

    public async Task<string> GetImageByIdAsync(Guid userId)
    => new(await postRepository.GetImageByIdAsync(userId));

    public async Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds, CancellationToken cancellationToken = default)
    {
        DateTime? scheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        var postMedias = mediaAttachments?.Select(url => new PostMedia
        {
            Id = Guid.NewGuid(),
            PostId = Guid.Empty, // temporary, updated once Post.Id is known
            ImageUrl = url
        }).ToList();

        var postEvents = integrationIds.Select(id => new PostEvent
        {
            PostId = Guid.Empty, // temporary, updated once Post.Id is known
            UserPlatformIntegrationId = id,
            Status = scheduledFor.HasValue ? PostStatus.Scheduled : PostStatus.Draft,
            PlatformPostIdentifier = "",
            PlatformResponse = new { }
        }).ToList();

        var post = Post.Create(
            userId,
            messageContent,
            scheduledFor,
            postEvents,
            postMedias
        );

        foreach (var e in post.PostEvents)
        {
            e.PostId = post.Id;
        }

        if (post.PostMedias != null)
        {
            foreach (var m in post.PostMedias)
            {
                m.PostId = post.Id;
            }
        }

        return await postRepository.CreatePostAsync(post, cancellationToken);
    }

    public async Task<bool> UpdatePostAsync(Guid userId, Guid postId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds, CancellationToken cancellationToken = default)
    {
        Post? post = await postRepository.GetByIdAsync(postId, cancellationToken);

        if (post == null || post.UserId != userId)
        {
            throw new UnauthorizedAccessException("Post does not belong to the user.");
        }

        if (!string.IsNullOrWhiteSpace(messageContent))
            post.MessageContent = messageContent;

        post.ScheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        post.PostMedias = mediaAttachments != null
            ? [.. mediaAttachments.Select(url => new PostMedia
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                ImageUrl = url
            })]
            : null;

        if (integrationIds != null && integrationIds.Length > 0)
        {
            post.PostEvents = [.. integrationIds.Select(id => new PostEvent
            {
                PostId = post.Id,
                UserPlatformIntegrationId = id,
                Status = PostStatus.Scheduled,
                PlatformPostIdentifier = "",
                PlatformResponse = new { }
            })];
        }

        post.UpdatedAt = DateTime.UtcNow;

        return await postRepository.UpdatePostAsync(post, cancellationToken);
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
