﻿
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

}

