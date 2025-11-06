using RippleSync.Domain.Platforms;

namespace Infrastructure.FakePlatform;

public static class FakePlatformInMemoryData
{
    public static List<PostData> PostData { get; private set; } = [];
    public static Dictionary<PostStatKey, PostStats> PostStats { get; private set; } = [];
}

public sealed record PostStatKey(Platform Platform, Guid PostId);

public sealed record PostData(
    Guid Id,
    string Content,
    DateTime PostedOn,
    IEnumerable<PostDataMedia> Media)
{
    public int DaysSincePosted => (DateTime.UtcNow - PostedOn).Days + 1;
}

public sealed record PostDataMedia(Guid Id);

public sealed record PostStats(
    int Days,
    int Likes,
    int Reach,
    int Engagement);