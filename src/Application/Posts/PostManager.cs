
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Responses;
using RippleSync.Domain.Users;
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

    public async Task<ListResponse<GetPostsByUserResponse>> GetImageByIdAsync(Guid userId, string? status)
    => new(await _postRepository.GetImageByIdAsync(userId, status));

    public async Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationIds)
        => await _postRepository.CreatePostAsync(userId, messageContent, timestamp, mediaAttachments, integrationIds);

    public async Task<bool> UpdatePostAsync(int postId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationIds)
    => await _postRepository.UpdatePostAsync(postId, messageContent, timestamp, mediaAttachments, integrationIds);


}
