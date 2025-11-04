namespace RippleSync.Application.Posts;

public sealed record TotalStatsForPlatform(
    string Platform,
    int PublishedPosts,
    int Reach,
    int Likes,
    double AverageEngagement);
