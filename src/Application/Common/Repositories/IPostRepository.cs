using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default);

    Task DeletePostAsync(Post post, CancellationToken cancellationToken = default);
    Task<Post> GetPostByIdAsync(Guid postId, CancellationToken cancellationToken = default);
}
