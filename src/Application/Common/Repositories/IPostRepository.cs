using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task DeleteAsync(Post post, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdatePostAsync(Post post, CancellationToken cancellationToken = default);
}
