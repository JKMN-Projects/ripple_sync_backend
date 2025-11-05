using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default);

    Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);

    Task CreatePostAsync(Post post, CancellationToken cancellationToken = default);

    Task UpdatePostAsync(Post post, CancellationToken cancellationToken = default);

    Task DeleteAsync(Post post, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default);
    Task<PostEvent> UpdatePostEventStatusAsync(PostEvent postEvent, CancellationToken cancellationToken = default);
}
