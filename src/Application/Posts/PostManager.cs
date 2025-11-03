
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Posts;

public class PostManager(
    IPostRepository postRepository,
    IPostQueries postQueries,
    IPlatformFactory platformFactory, ILogger<PostManager> logger)
{
    public async Task<ListResponse<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
        => new(await postQueries.GetPostsByUserAsync(userId, status, cancellationToken));

    public async Task<TotalPostStatsResponse> GetPostStatForPeriodAsync(Guid userId, DateTime from, CancellationToken cancellationToken = default)
    {
        // TODO: Why is Status required here?
        IEnumerable<GetPostsByUserResponse> posts = await postQueries.GetPostsByUserAsync(userId, null, cancellationToken);
        IEnumerable<Integration> userIntegrations = []; // TODO: Get user integrations 

        foreach (var integration in userIntegrations)
        {
            IPlatform platform = platformFactory.Create(integration.Platform);
            PlatformStats stats = await platform.GetInsightsFromIntegrationAsync(integration);
        }

        int publishedPosts = posts.Count(p => p.StatusName.Equals("Published", StringComparison.OrdinalIgnoreCase));
        int scheduledPosts = posts.Count(p => p.StatusName.Equals("Scheduled", StringComparison.OrdinalIgnoreCase));

        // TODO: Calculate TotalReach and TotalLikes
        return new TotalPostStatsResponse(
            PublishedPosts: publishedPosts,
            ScheduledPosts: scheduledPosts,
            TotalReach: -1,
            TotalLikes: -1);
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
    public async Task<IEnumerable<Post>> GetPostReadyToPublish(CancellationToken cancellationToken = default)
    {
        IEnumerable<Post> posts = await postRepository.GetPostsReadyToPublish(cancellationToken);

        IEnumerable<Post> postsReadyToPost = posts.Where(p => p.IsReadyToPublish());
        return posts;
    }
    public async Task<PostEvent> UpdatePostEventAsync(PostEvent postEvent) => await postRepository.UpdatePostEventStatus(postEvent);

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

        List<Guid> userPlatformIntegrations = post.PostEvents.Select(pe => pe.UserPlatformIntegrationId).ToList();

        //TODO: Get List of UserPlatformintegration from list of guids
        List<Integration> integrations = []; // _integrationManager.GetIntegrationsByIds(userPlatformIntegrations);

        //Request platform
        foreach (var integration in integrations)
        {
            logger.LogInformation(
                    "Started publish for post event: PostId={PostId}, Platform={Platform}",
                    post.Id,
                    integration.Platform);
            IPlatform platform = platformFactory.Create(integration.Platform);
            var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id);
            try
            {
                var responsePostEvent = await platform.PublishPostAsync(post, integration);
                await UpdatePostEventAsync(responsePostEvent);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to publish post for : PostId={PostId}, Platform={Platform}, Exception {Exception}",
                    post.Id,
                    integration.Platform,
                    ex.Message
                );
                postEvent.Status = PostStatus.Failed;
                await UpdatePostEventAsync(postEvent);
            }
        }
        logger.LogInformation(
            "Post has been published: PostId={PostId}",
            post.Id);

    }
}
