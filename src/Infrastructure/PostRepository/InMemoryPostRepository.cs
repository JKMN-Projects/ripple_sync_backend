
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository, IPostQueries
{
    /// <summary>
    /// In-Memory implementation of GetPostsByUserAsync
    /// Does not check userId in this in-memory implementation
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="status"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {
        var post = InMemoryData.Posts.Where(post =>
            status is null ||
            post.PostEvents.MaxBy(pe => pe.Status)!.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)
        );

        var response = post.Select(p => new GetPostsByUserResponse(
            p.Id,
            p.MessageContent,
            p.PostEvents.MaxBy(pe => pe.Status)!.Status.ToString(),
            [],
            p.ScheduledFor.HasValue ? new DateTimeOffset(p.ScheduledFor.Value).ToUnixTimeMilliseconds() : null,
            ["Facebook", "LinkedIn"]
        ));

        return Task.FromResult(response);
    }
    public Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        var postToDelete = InMemoryData.Posts.Single(p => p.Id == post.Id);

        InMemoryData.Posts.Remove(postToDelete);
        return Task.CompletedTask;
    }
    public Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var postEntity = InMemoryData.Posts.SingleOrDefault(p => p.Id == postId);
        return Task.FromResult<Post>(postEntity);
    }

    public Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var imageUrl = InMemoryData.Posts
            .SelectMany(p => p.PostMedias)
            .Where(pm => pm.Id == imageId)
            .Select(pm => pm.ImageUrl)
            .FirstOrDefault();

        return Task.FromResult(imageUrl ?? "");
    }


    public async Task<bool> CreatePostAsync(
        Guid userId,
        string messageContent,
        long? timestamp,
        string[]? mediaAttachments,
        Guid[] integrationsIds,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        DateTime? scheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        var postMedias = mediaAttachments?.Select(url => new PostMedia
        {
            Id = Guid.NewGuid(),
            PostId = Guid.Empty, // temporary, updated below once Post.Id is known
            ImageUrl = url
        }).ToList();

        var postEvents = integrationsIds.Select(id => new PostEvent
        {
            PostId = Guid.Empty, // temporary, updated below once Post.Id is known
            UserPlatformIntegrationId = id,
            Status = scheduledFor.HasValue ? PostStatus.Scheduled : PostStatus.Draft,
            PlatformPostIdentifier = "",
            PlatformResponse = new { CreatedAt = DateTime.UtcNow }
        }).ToList();

        var post = Post.Create(
            userId,
            messageContent,
            scheduledFor,
            postEvents,
            postMedias
        );

        foreach (var e in post.PostEvents)
            e.PostId = post.Id;

        if (post.PostMedias != null)
        {
            foreach (var m in post.PostMedias)
                m.PostId = post.Id;
        }

        InMemoryData.Posts.Add(post);

        return true;
    }

    public async Task<bool> UpdatePostAsync(
        Guid postId,
        string messageContent,
        long? timestamp,
        string[]? mediaAttachments,
        Guid[] integrationsIds,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        Post? post = InMemoryData.Posts.FirstOrDefault(p => p.Id == postId);

        if (post == null)
            return false;

        if (!string.IsNullOrWhiteSpace(messageContent))
            post.MessageContent = messageContent;

        post.ScheduledFor = timestamp.HasValue
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp.Value).UtcDateTime
            : null;

        if (mediaAttachments != null)
        {
            post.PostMedias = [.. mediaAttachments.Select(url => new PostMedia
            {
                Id = Guid.NewGuid(),
                PostId = post.Id,
                ImageUrl = url
            })];
        }

        if (integrationsIds != null && integrationsIds.Length > 0)
        {
            post.PostEvents = [.. integrationsIds.Select(id => new PostEvent
            {
                PostId = post.Id,
                UserPlatformIntegrationId = id,
                Status = PostStatus.Scheduled,
                PlatformPostIdentifier = "",
                PlatformResponse = new { UpdatedAt = DateTime.UtcNow }
            })];
        }

        post.UpdatedAt = DateTime.UtcNow;

        return true;
    }
}

