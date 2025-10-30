
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository
{


    private static Guid _userId = Guid.Parse("a9856986-14e4-464b-acc7-dcb84ddf9f36");
    private static readonly List<Post> _postEntities =
    [
        new (_userId,"My first post",DateTime.UtcNow, DateTime.UtcNow.AddDays(2),
        [
            new() {
                PostId = Guid.NewGuid(),
                UserPlatformIntegrationId = Guid.NewGuid(),
                Status = PostStatus.Posted,
                PlatformPostIdentifier = "123456",
                PlatformResponse = null
            }
        ]),
        new (_userId,"My Scheduled post",DateTime.UtcNow, DateTime.UtcNow.AddDays(5),[
            new() {
                PostId = Guid.NewGuid(),
                UserPlatformIntegrationId = Guid.NewGuid(),
                Status = PostStatus.Scheduled,
                PlatformPostIdentifier = "654321",
                PlatformResponse = null
            }
        ]),
        new (_userId,"Stuck while processing",DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2),[
            new() {
                PostId = Guid.NewGuid(),
                UserPlatformIntegrationId = Guid.NewGuid(),
                Status = PostStatus.Processing,
                PlatformPostIdentifier = "",
                PlatformResponse = null
            }
        ]),
        new (_userId,"My post will not upload",DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2),[
            new() {
                PostId = Guid.NewGuid(),
                UserPlatformIntegrationId = Guid.NewGuid(),
                Status = PostStatus.Failed,
                PlatformPostIdentifier = "",
                PlatformResponse = "Error"
            }
        ]),
        new (_userId,"Just created this post - NOT DONE",DateTime.UtcNow, null,[
            new() {
                PostId = Guid.NewGuid(),
                UserPlatformIntegrationId = Guid.NewGuid(),
                Status = PostStatus.Draft,
                PlatformPostIdentifier = "",
                PlatformResponse = null
            }
        ])
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

}

