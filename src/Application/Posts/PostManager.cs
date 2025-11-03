
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Domain.Users;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Posts;

public class PostManager(
    IPostRepository postRepository,
    IPostQueries postQueries,
    IPlatformFactory platformFactory)
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
    public async Task<ListResponse<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status)
        => new(await postRepository.GetPostsByUserAsync(userId, status));

    public async Task<string> GetImageByIdAsync(Guid userId)
    => new(await postRepository.GetImageByIdAsync(userId));

    public async Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds)
        => await postRepository.CreatePostAsync(userId, messageContent, timestamp, mediaAttachments, integrationIds);

    public async Task<bool> UpdatePostAsync(Guid postId, string messageContent, long? timestamp, string[]? mediaAttachments, Guid[] integrationIds)
    => await postRepository.UpdatePostAsync(postId, messageContent, timestamp, mediaAttachments, integrationIds);

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
