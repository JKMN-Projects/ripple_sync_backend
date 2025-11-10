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
        public Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task CreateAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task UpdateAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task DeleteAsync(Post post, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdatePostEventStatusAsync(PostEvent postEvent, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RemoveScheduleOnAllPostsWithoutEvent(Guid userId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public sealed class Composite : IPostRepository
    {
        private readonly IPostRepository[] _repositories;
        public Composite(params IPostRepository[] repositories)
        {
            _repositories = repositories;
        }

        private Task TryMethod(Func<IPostRepository, Task> composedMethod)
        {
            foreach (var repository in _repositories)
            {
                try
                {
                    return composedMethod(repository);
                }
                catch (NotImplementedException)
                {
                    // Log or handle the exception as needed
                }
            }
            throw new InvalidOperationException("All repositories failed.");
        }

        private Task<T> TryMethod<T>(Func<IPostRepository, Task<T>> composedMethod)
        {
            foreach (var repository in _repositories)
            {
                try
                {
                    return composedMethod(repository);
                }
                catch (NotImplementedException)
                {
                    // Log or handle the exception as needed
                }
            }
            throw new InvalidOperationException("All repositories failed.");
        }

        public Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.GetAllByUserIdAsync(userId, cancellationToken));

        public Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.GetByIdAsync(postId, cancellationToken));
        public Task CreateAsync(Post post, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.CreateAsync(post, cancellationToken));

        public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.UpdateAsync(post, cancellationToken));

        public Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.DeleteAsync(post, cancellationToken));

        public Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.GetPostsReadyToPublishAsync(cancellationToken));
        public Task RemoveScheduleOnAllPostsWithoutEvent(Guid userId, CancellationToken cancellationToken = default)
            => TryMethod(repo => repo.RemoveScheduleOnAllPostsWithoutEvent(userId, cancellationToken));
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

        public static class GetByIdAsync
        {
            public class ReturnsSpecifiedPost : Dummy
            {
                private readonly Post _post;
                public ReturnsSpecifiedPost(Post post)
                {
                    _post = post;
                }
                public override Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
                    => Task.FromResult<Post?>(_post);
            }

            public class ReturnsNull : Dummy
            {
                public override Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
                    => Task.FromResult<Post?>(null);
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
                    postMedia: post.PostMedia,
                    postsEvents: post.PostEvents)
                );
                OnInvokation?.Invoke(post, this);
                await _spied.UpdateAsync(post, cancellationToken);
            }
        }
    }
}