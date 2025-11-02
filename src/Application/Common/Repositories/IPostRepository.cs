using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task DeleteAsync(Post post, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Post>> GetPostsReadyToPublish(CancellationToken cancellationToken = default);
    Task<PostEvent> UpdatePostEventStatus(PostEvent postEvent, CancellationToken cancellationToken = default);
}
