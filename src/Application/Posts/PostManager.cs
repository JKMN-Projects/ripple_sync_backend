
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
namespace RippleSync.Application.Posts;

public class PostManager
{
    private readonly IPostRepository _postRepository;
    public PostManager(IPostRepository postRepository)
    {
        _postRepository = postRepository;
    }
    public async Task<ListResponse<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId)
        => new(await _postRepository.GetPostsByUserAsync(userId));


}
