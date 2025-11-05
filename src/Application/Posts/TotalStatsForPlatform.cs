namespace RippleSync.Application.Posts;

public sealed record TotalStatsForPlatform(
    string Platform,
    int PublishedPosts,
    int Reach,
    int Engagement,
    int Likes,
    double AverageEngagement);
