using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default);

    Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(Guid postId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default);

    Task DeletePost(Post post);

    Task<Post> GetPostById(Guid postId);
}
