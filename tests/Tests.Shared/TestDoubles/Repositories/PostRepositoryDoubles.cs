using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Tests.Shared.TestDoubles.Repositories;

public static class PostRepositoryDoubles
{
    public class Dummy : IPostRepository
    {
        public virtual Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task CreateAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task UpdateAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task DeleteAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdatePostEventStatusAsync(PostEvent postEvent, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public static class Stubs
    {
        public static class UpdateAsync
        {
            public class DoesNothing : Dummy
            {
                public override Task UpdateAsync(Post post, CancellationToken cancellationToken = default) 
                    => Task.CompletedTask;
            }
        }
    }

    public static class Spies
    {
        public class UpdateAsyncSpy : Dummy
        {
            private readonly IPostRepository _spied;
            private readonly List<Post> _updatedPosts = [];

            public int InvocationCount { get; private set; }
            public IReadOnlyList<Post> UpdatedPosts => _updatedPosts.AsReadOnly();
            public Post? LatestUpdated => _updatedPosts.LastOrDefault();

            public Action<Post, UpdateAsyncSpy>? OnInvokation { get; set; }

            public UpdateAsyncSpy(IPostRepository spied)
            {
                _spied = spied;
            }

            public override async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
            {
                InvocationCount++;
                _updatedPosts.Add(Post.Reconstitute(
                    id: post.Id,
                    userId: post.UserId,
                    messageContent: post.MessageContent,
                    submittedAt: post.SubmittedAt,
                    updatedAt: post.UpdatedAt,
                    scheduledFor: post.ScheduledFor,
                    postMedias: post.PostMedias,
                    postsEvents: post.PostEvents)
                );
                OnInvokation?.Invoke(post, this);
                await _spied.UpdateAsync(post, cancellationToken);
            }
        }
    }
}