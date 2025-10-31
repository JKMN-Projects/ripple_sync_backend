using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId,string? status, CancellationToken cancellationToken = default);
    Task DeletePost(Post post);
    Task<Post> GetPostById(Guid postId);
}
