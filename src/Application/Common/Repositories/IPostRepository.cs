using RippleSync.Application.Posts;

namespace RippleSync.Application.Common.Repositories;

public interface IPostRepository
{
    Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default);

    Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default);

    Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default);

    Task<bool> UpdatePostAsync(int postId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default);
}
