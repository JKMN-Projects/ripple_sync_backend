using RippleSync.Application.Posts;

namespace RippleSync.Application.Common.Queries;

public interface IPostQueries
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default);
}