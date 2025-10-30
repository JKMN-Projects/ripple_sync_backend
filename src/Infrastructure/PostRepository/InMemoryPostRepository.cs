
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository
{

    private static Guid _userId = Guid.NewGuid();
    private static readonly List<Post> _postEntities =
    [
        new (Guid.NewGuid(),"My first post",DateTime.UtcNow, DateTime.UtcNow.AddDays(2),
        [
            new() {
                PostId = Guid.NewGuid(),
                UserId = _userId,
                Status = PostStatus.Posted,
                PlatformPostIdentifier = "123456",
                PlatformResponse = null
            }
        ],
        null),
        new (Guid.NewGuid(),"My Scheduled post",DateTime.UtcNow, DateTime.UtcNow.AddDays(5),[
            new() {
                PostId = Guid.NewGuid(),
                UserId = _userId,
                Status = PostStatus.Scheduled,
                PlatformPostIdentifier = "654321",
                PlatformResponse = null
            }
        ],
        null),
        new (Guid.NewGuid(),"Stuck while processing",DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2),[
            new() {
                PostId = Guid.NewGuid(),
                UserId = _userId,
                Status = PostStatus.Processing,
                PlatformPostIdentifier = "",
                PlatformResponse = null
            }
        ],
        null),
        new (Guid.NewGuid(),"My post will not upload",DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2),[
            new() {
                PostId = Guid.NewGuid(),
                UserId = _userId,
                Status = PostStatus.Failed,
                PlatformPostIdentifier = "",
                PlatformResponse = "Error"
            }
        ],
        null),
        new (Guid.NewGuid(),"Just created this post - NOT DONE",DateTime.UtcNow, null,[
            new() {
                PostId = Guid.NewGuid(),
                UserId = _userId,
                Status = PostStatus.Draft,
                PlatformPostIdentifier = "",
                PlatformResponse = null
            }
        ],
        null)
    ];

    /// <summary>
    /// In-Memory implementation of GetPostsByUserAsync
    /// Does not check userId in this in-memory implementation
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="status"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {

        var post = _postEntities.Where(post =>
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

        return response;
    }
    public Task DeletePost(Post post)
    {

        var postToDelete = _postEntities.Single(p => p.Id == post.Id);

        _postEntities.Remove(postToDelete);
        return Task.CompletedTask;
    }
    public async Task<Post> GetPostById(Guid postId)
    {
        var postEntity = _postEntities.SingleOrDefault(p => p.Id == postId);
        return postEntity;
    }

    public Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var imageUrl = _postEntities
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
        int[] integrationsIds,
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
            UserId = userId,
            Status = scheduledFor.HasValue ? PostStatus.Scheduled : PostStatus.Draft,
            PlatformPostIdentifier = "",
            PlatformResponse = new { CreatedAt = DateTime.UtcNow }
        }).ToList();

        var post = new Post(
            userId,
            messageContent,
            DateTime.UtcNow,
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

        _postEntities.Add(post);

        return true;
    }

    public async Task<bool> UpdatePostAsync(
        Guid postId,
        string messageContent,
        long? timestamp,
        string[]? mediaAttachments,
        int[] integrationsIds,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        Post? post = _postEntities.FirstOrDefault(p => p.Id == postId);

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
                UserId = post.UserId,
                Status = PostStatus.Scheduled,
                PlatformPostIdentifier = "",
                PlatformResponse = new { UpdatedAt = DateTime.UtcNow }
            })];
        }

        post.UpdatedAt = DateTime.UtcNow;

        return true;
    }

    private static PostStatus GetPostStatus(long? timestamp)
        => timestamp == null
        ? PostStatus.Draft
        : timestamp > new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()
        ? PostStatus.Scheduled
        : PostStatus.Posted;

    private static List<string> GetPlatformStringArray(int[] integrationsIds)
    {
        List<string> platforms = [];

        foreach (int id in integrationsIds)
        {
            platforms.Add(GetPlatformName(id));
        }

        return platforms;
    }

    private static string GetPlatformName(int id)
    {
        return id switch
        {
            1 => "X",
            2 => "Facebook",
            3 => "LinkedIn",
            4 => "Instagram",
            5 => "YouTube",
            _ => "",
        };
    }
}

