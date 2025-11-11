
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Posts;

public class PostManager(
    ILogger<PostManager> logger,
    IUnitOfWork unitOfWork,
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
            if (!posts.Any(p => p.PostEvents.Any(pe => pe.UserPlatformIntegrationId == integration.Id)))
            {
                logger.LogInformation("No posts found for Integration ID {IntegrationId}. Skipping stats retrieval for this integration.", integration.Id);
                platformStats.Add((platformName: integration.Platform.ToString(), PlatformStats.Empty));
                continue;
            }

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
                    : 0,
                IsSimulated: ps.stats.IsSimulated)
            )
        );
    }

    public async Task<string?> GetImageByIdAsync(Guid userId)
        => new(await postQueries.GetImageByIdAsync(userId));

    public async Task CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[]? integrationIds, CancellationToken cancellationToken = default)
    {
        DateTime? scheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        var postMedias = mediaAttachments?
            .Select(PostMedia.Create)
            .ToList() ?? [];

        List<PostEvent> postEvents = [];

        if (scheduledFor.HasValue)
        {
            postEvents = integrationIds?
                .Select(id => PostEvent.Create(id, PostStatus.Scheduled, null, null))
                .ToList() ?? [];
        }

        var post = Post.Create(
            userId,
            messageContent,
            scheduledFor,
            postMedias,
            postEvents
        );

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await postRepository.CreateAsync(post, cancellationToken));
    }

    public async Task UpdatePostAsync(Guid userId, Guid postId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[]? integrationIds, CancellationToken cancellationToken = default)
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

        post.PostMedia = mediaAttachments?
            .Select(PostMedia.Create)
            .ToList() ?? [];

        if (post.ScheduledFor.HasValue)
        {
            post.PostEvents = integrationIds?
                .Select(id => PostEvent.Create(id, PostStatus.Scheduled, null, null))
                .ToList() ?? [];
        }
        else
        {
            post.PostEvents = [];
        }

        post.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await postRepository.UpdateAsync(post, cancellationToken));
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

        if (!post.IsDeletable())
        {
            throw new InvalidOperationException("Post cannot be deleted in its current state.");
        }

        // Then delete

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await postRepository.DeleteAsync(post, cancellationToken));
    }

    public async Task<IEnumerable<Post>> GetPostReadyToPublish(CancellationToken cancellationToken = default)
    {
        IEnumerable<Post> posts = await postRepository.GetPostsReadyToPublishAsync(cancellationToken);

        IEnumerable<Post> postsReadyToPost = posts.Where(p => p.IsReadyToPublish());

        return postsReadyToPost;
    }

    public async Task ProcessPostAsync(Post post, CancellationToken cancellationToken = default)
    {
        if (post.PostEvents.Count == 0)
        {
            logger.LogInformation(
                "Post has no post events to process: PostId={PostId}",
                post.Id);
            return;
        }

        logger.LogInformation(
            "Processing post: PostId={PostId}",
            post.Id);

        IEnumerable<Guid> userPlatformIntegrations = post.PostEvents.Select(pe => pe.UserPlatformIntegrationId);
        IEnumerable<Integration> integrations;
        try
        {
            integrations = await integrationRepository.GetByIdsAsync(userPlatformIntegrations, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve integrations for post: PostId={PostId}", post.Id);
            foreach (var postEvent in post.PostEvents)
            {
                postEvent.Status = PostStatus.Failed;
            }
            throw;
        }

        // Materialize FIRST, before any modifications
        post.PostEvents = [.. post.PostEvents];

        foreach (var postEvent in post.PostEvents)
        {
            try
            {
                var integration = integrations.FirstOrDefault(i => i.Id == postEvent.UserPlatformIntegrationId)
                    ?? throw new InvalidOperationException("No integration found. PostEvent: PostId={PostId}, UserPlatformIntegrationId={UserPlatformIntegrationId}");

                ISoMePlatform platform = platformFactory.Create(integration.Platform);
                await platform.PublishPostAsync(post, integration);
                postEvent.Status = PostStatus.Posted;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PostEvent ({PostId}) failed when attempting to publish to UserPlatformIntegrationId ({UserPlatformIntegrationId})",
                    post.Id,
                    postEvent.UserPlatformIntegrationId);
                postEvent.Status = PostStatus.Failed;
            }
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await postRepository.UpdateAsync(post, cancellationToken));

        logger.LogInformation(
            "Post has been published: PostId={PostId}",
            post.Id);
    }

    public async Task RetryPublishAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Retrying publish for post: PostId={PostId}",
            postId);

        Post? post = await postRepository.GetByIdAsync(postId, cancellationToken)
            ?? throw EntityNotFoundException.ForEntity<Post>(postId, nameof(Post.Id));

        if (post.UserId != userId)
            throw new UnauthorizedException("Post does not belong to the user.");

        if (post.PostEvents.Count == 0)
            throw new InvalidOperationException("Post has no post events to retry.");

        var failedPosts = post.PostEvents.Where(pe => pe.Status == PostStatus.Failed).ToList();

        if (failedPosts.Count == 0)
        {
            logger.LogInformation(
                "No failed post events to retry for post: PostId={PostId}",
                postId);
            return;
        }

        foreach (var postEvent in failedPosts)
        {
            postEvent.Status = PostStatus.Scheduled;
        }

        await unitOfWork.ExecuteInTransactionAsync(async () =>
            await postRepository.UpdateAsync(post, cancellationToken));
    }
}
