using RippleSync.Application.Common.Queries;
using RippleSync.Application.Posts;

namespace RippleSync.Tests.Common.TestDoubles.Queries;

public static class PostQueriesDoubles
{
    public class Dummy : IPostQueries
    {
        public virtual Task<string?> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
