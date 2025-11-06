
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
        IEnumerable<Post> posts = await postRepository.GetAllByUserIdAsync(userId, cancellationToken);
        IEnumerable<Integration> userIntegrations = await integrationRepository.GetByUserIdAsync(userId, cancellationToken);

        List<(string platformName, PlatformStats stats)> platformStats = [];

        foreach (var integration in userIntegrations)
        {
            ISoMePlatform? platform = null;
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
            PlatformStats stats = await platform.GetInsightsFromIntegrationAsync(integration, posts.Where(p => p.PostEvents.Any(pe => pe.UserPlatformIntegrationId == integration.Id)));
            platformStats.Add((platformName: integration.Platform.ToString(), stats));
        }
        int publishedPosts = posts.Count(p => p.GetPostMaxStatus() == PostStatus.Posted);
        int scheduledPosts = posts.Count(p => p.GetPostMaxStatus() == PostStatus.Scheduled);

        return new TotalPostStatsResponse(
            PublishedPosts: publishedPosts,
            ScheduledPosts: scheduledPosts,
            TotalReach: platformStats.Sum(x => x.stats.Reach),
            TotalLikes: platformStats.Sum(x => x.stats.Likes),
            TotalStatsForPlatforms: platformStats.Select(ps => new TotalStatsForPlatform(
                Platform: ps.platformName,
                PublishedPosts: ps.stats.PostCount,
                Reach: ps.stats.Reach,
                Engagement: ps.stats.Engagement,
                Likes: ps.stats.Likes,
                AverageEngagement: ps.stats.Engagement > 0 && ps.stats.PostCount > 0
                    ? (double)ps.stats.Engagement / ps.stats.PostCount
                    : 0)).ToList()
        );
    }

    public async Task<string?> GetImageByIdAsync(Guid userId)
        => new(await postQueries.GetImageByIdAsync(userId));

    public async Task CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds, CancellationToken cancellationToken = default)
    {
        DateTime? scheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        var postMedias = mediaAttachments?
            .Select(PostMedia.New)
            .ToList();

        var postEvents = integrationIds
            .Select(id => PostEvent.Create(id, scheduledFor.HasValue ? PostStatus.Scheduled : PostStatus.Draft, "", new { }))
            .ToList();

        var post = Post.Create(
            userId,
            messageContent,
            scheduledFor,
            postMedias,
            postEvents
        );

        await postRepository.CreateAsync(post, cancellationToken);
    }

    public async Task UpdatePostAsync(Guid userId, Guid postId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds, CancellationToken cancellationToken = default)
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

        if (mediaAttachments != null)
        {
            post.PostMedias = [.. mediaAttachments.Select(PostMedia.New)];
        }

        if (integrationIds != null && integrationIds.Length > 0)
        {
            post.PostEvents = [.. integrationIds.Select(id => PostEvent.Create(id, PostStatus.Scheduled, "", new { }))];
        }

        post.UpdatedAt = DateTime.UtcNow;

        await postRepository.UpdateAsync(post, cancellationToken);
    }

    public async Task DeletePostByIdAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        //Request post first
        Post? post = await postRepository.GetByIdAsync(postId, cancellationToken);

        // Check if post belongs to user and if its deletable
        if (post == null || post.UserId != userId)
        {
            throw new UnauthorizedException("Post does not belong to the user.");
        }
        if (post.IsDeletable() is false)
        {
            throw new InvalidOperationException("Post cannot be deleted in its current state.");
        }

        // Then delete
        await postRepository.DeleteAsync(post, cancellationToken);
    }
    public async Task<IEnumerable<Post>> GetPostReadyToPublish(CancellationToken cancellationToken = default)
    {
        IEnumerable<Post> posts = await postRepository.GetPostsReadyToPublishAsync(cancellationToken);

        IEnumerable<Post> postsReadyToPost = posts.Where(p => p.IsReadyToPublish());
        return posts;
    }

    public async Task<PostEvent> UpdatePostEventAsync(PostEvent postEvent)
        => await postRepository.UpdatePostEventStatusAsync(postEvent);

    public async Task ProcessPostEventAsync(Post post, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing post: PostId={PostId}",
            post.Id);

        foreach (var postEvent in post.PostEvents)
        {
            postEvent.Status = PostStatus.Processing;
            await UpdatePostEventAsync(postEvent);
        }

        IEnumerable<Guid> userPlatformIntegrations = post.PostEvents.Select(pe => pe.UserPlatformIntegrationId);

        IEnumerable<Integration> integrations = await integrationRepository.GetIntegrationsByIdsAsync(userPlatformIntegrations, cancellationToken);

        //Request platform
        foreach (var integration in integrations)
        {
            logger.LogInformation(
                    "Started publish for post event: PostId={PostId}, Platform={Platform}",
                    post.Id,
                    integration.Platform);
            ISoMePlatform platform = platformFactory.Create(integration.Platform);
            try
            {
                var responsePostEvent = await platform.PublishPostAsync(post, integration);

                //If still processing, mark as posted
                if (responsePostEvent.Status == PostStatus.Processing)
                {
                    responsePostEvent.Status = PostStatus.Posted;
                }
                await UpdatePostEventAsync(responsePostEvent);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to publish post for : PostId={PostId}, Platform={Platform}, Exception {Exception}",
                    post.Id,
                    integration.Platform,
                    ex.Message
                );
                var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id);
                postEvent!.Status = PostStatus.Failed;
                await UpdatePostEventAsync(postEvent);
            }
        }
        logger.LogInformation(
            "Post has been published: PostId={PostId}",
            post.Id);

    }
}
