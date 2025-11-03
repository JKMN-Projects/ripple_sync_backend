namespace RippleSync.Application.Posts;

public sealed record TotalPostStatsResponse(
    int PublishedPosts,
    int ScheduledPosts,
    int TotalReach,
    int TotalLikes);

public sealed record TotalStatsForPlatform(
    string Platform,
    int PublishedPosts,
    int Reach,
    int Likes,
    double AverageEngagement,
    int Followers);
