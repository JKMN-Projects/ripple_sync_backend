
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository
{
    private static readonly List<GetPostsByUserResponse> _posts =
    [
        new  (1,"My first post","Posted",[],1761571800000,["Instagram","Facebook"]),
        new  (1,"My Scheduled post","Scheduled",[],1761571800000,["Instagram"])
    ];
    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => _posts;
}

