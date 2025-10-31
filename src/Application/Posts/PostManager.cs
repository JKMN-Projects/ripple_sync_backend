
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
    public async Task<ListResponse<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status)
        => new(await _postRepository.GetPostsByUserAsync(userId, status));

    public async Task DeletePostByIdOnUser(Guid userId, Guid postId)
    {
        //Request post first
        var post = await _postRepository.GetPostByIdAsync(postId);

        // Check if post belongs to user and if its deletable
        if (post == null || post.UserId != userId)
        {
            throw new UnauthorizedAccessException("Post does not belong to the user.");
        }
        if (post.IsDeletable() is false)
        {
            throw new InvalidOperationException("Post cannot be deleted in its current state.");
        }

        // Then delete
        await _postRepository.DeletePost(post);
    }




}
