using RippleSync.Application.Common.Responses;
using RippleSync.Application.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
