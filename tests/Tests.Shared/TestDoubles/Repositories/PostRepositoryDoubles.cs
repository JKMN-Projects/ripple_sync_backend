using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Tests.Shared.TestDoubles.Repositories;

public static class PostRepositoryDoubles
{
    public class Dummy : IPostRepository
    {
        public virtual Task<bool> CreatePostAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task DeleteAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<Post>> GetPostsReadyToPublish(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<bool> UpdatePostAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<PostEvent> UpdatePostEventStatus(PostEvent postEvent, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}