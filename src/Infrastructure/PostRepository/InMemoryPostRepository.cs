
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Posts;

namespace RippleSync.Infrastructure.PostRepository;

internal class InMemoryPostRepository : IPostRepository
{
    private static List<GetPostsByUserResponse> _posts =
    [
        new  (1,"My first post","Posted",[],1761571800000,["Instagram","Facebook"]),
        new  (2,"My Scheduled post","Scheduled",[],1761571800000,["Instagram"]),
        new  (3,"Stuck while processing","Processing",[],1761371800000,["Facebook"]),
        new  (4,"My post will not upload","Failed",[],1761371800000,["X","Youtube"])
    ];

    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {
        return _posts.Where(p => status is null || p.StatusName.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    public Task<string> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
    }


    public async Task<bool> CreatePostAsync(Guid userId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default)
    {
        GetPostsByUserResponse post = new GetPostsByUserResponse(_posts.Count + 1, messageContent, GetStatusName(timestamp), mediaAttachments ?? [], timestamp ?? 0, [.. GetPlatformStringArray(integrationsIds)]);

        _posts.Add(post);

        return true;
    }

    public async Task<bool> UpdatePostAsync(int postId, string messageContent, long? timestamp, string[]? mediaAttachments, int[] integrationsIds, CancellationToken cancellationToken = default)
    {
        GetPostsByUserResponse post = new GetPostsByUserResponse(postId, messageContent, GetStatusName(timestamp), mediaAttachments ?? [], timestamp ?? 0, [.. GetPlatformStringArray(integrationsIds)]);

        if (post == null) return false;

        _posts = [.. _posts.Select(p => p.id == postId ? post : p)];

        return true;
    }

    private static string GetStatusName(long? timestamp) => timestamp == null ? "Draft" : timestamp > new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() ? "Scheduled" : "Posted";

    private static List<string> GetPlatformStringArray(int[] integrationsIds)
    {
        List<string> platforms = [];

        foreach (int id in integrationsIds)
        {
            platforms.Add(GetPlatformName(id));
        }

        return platforms;
    }

    private static string GetPlatformName(int id)
    {
        return id switch
        {
            1 => "X",
            2 => "Facebook",
            3 => "LinkedIn",
            4 => "Instagram",
            5 => "YouTube",
            _ => "",
        };
    }
}

