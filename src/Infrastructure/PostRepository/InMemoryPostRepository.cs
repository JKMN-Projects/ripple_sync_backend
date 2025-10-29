
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository
{
    private static readonly List<GetPostsByUserResponse> _posts =
    [
        new  (1,"My first post","Posted",[],1761571800000,["Instagram","Facebook"]),
        new  (2,"My Scheduled post","Scheduled",[],1761571800000,["Instagram"]),
        new  (3,"Stuck while processing","Processing",[],1761371800000,["Facebook"]),
        new  (4,"My post will not upload","Failed",[],1761371800000,["X","Youtube"])
    ];
    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId,string? status, CancellationToken cancellationToken = default)
    {
        return _posts.Where(p => status is null || p.StatusName.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

}

