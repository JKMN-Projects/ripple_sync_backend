namespace RippleSync.Application.Posts;

public sealed record TotalPostStatsResponse(
    int PublishedPosts,
    int ScheduledPosts,
    int TotalReach,
    int TotalLikes,
    IEnumerable<TotalStatsForPlatform> TotalStatsForPlatforms);

public sealed record TotalStatsForPlatform(
    string Platform,
    int PublishedPosts,
    int Reach,
    int Likes,
    double AverageEngagement);
