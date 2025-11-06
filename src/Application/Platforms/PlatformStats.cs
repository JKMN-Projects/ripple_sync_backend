namespace RippleSync.Application.Platforms;

public sealed record PlatformStats(
    int PostCount,
    int Reach,
    int Engagement,
    int Likes)
{
    public static PlatformStats Empty => new(0, 0, 0, 0);
}