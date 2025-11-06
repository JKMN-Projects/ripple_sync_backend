using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Data;

namespace Infrastructure.FakePlatform;

public class PostStatGenerator
{
    public static async Task<PlatformStats> CalculateAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform)
    {
        foreach (var post in publishedPostsOnPlatform)
        {
            await Task.Delay(Random.Shared.Next(80, 150));
            if (!FakePlatformInMemoryData.PostStats.TryGetValue(new PostStatKey(integration.Platform, post.Id), out var statsForPost))
            {
                statsForPost = new PostStats(
                    Days: 0,
                    Likes: 0,
                    Reach: 0,
                    Engagement: 0
                );
            }

            int daysAtPreviousCalculation = statsForPost.Days;
            int reach = statsForPost.Reach;
            int engagement = statsForPost.Engagement;
            int likes = statsForPost.Likes;

            int daysSincePosted = (DateTime.UtcNow - post.ScheduledFor)!.Value.Days + 1;
            if (daysAtPreviousCalculation >= daysSincePosted)
                continue;

            for (int i = daysAtPreviousCalculation; i < daysSincePosted; i++)
            {
                reach += Random.Shared.Next(35, 135);
                engagement += Random.Shared.Next(25, 70);
                likes += Random.Shared.Next(20, 65);
            }
            FakePlatformInMemoryData.PostStats[new PostStatKey(integration.Platform, post.Id)] = statsForPost with
            {
                Days = daysSincePosted,
                Likes = likes,
                Reach = reach,
                Engagement = engagement
            };
        }

        int postCount = publishedPostsOnPlatform.Count();
        int totalReach = FakePlatformInMemoryData.PostStats.Where(kv => kv.Key.Platform == integration.Platform).Sum(kv => kv.Value.Reach);
        int totalEngagement = FakePlatformInMemoryData.PostStats.Where(kv => kv.Key.Platform == integration.Platform).Sum(kv => kv.Value.Engagement);
        int totalLikes = FakePlatformInMemoryData.PostStats.Where(kv => kv.Key.Platform == integration.Platform).Sum(kv => kv.Value.Likes);

        return new PlatformStats(
            PostCount: postCount,
            Reach: totalReach,
            Engagement: totalEngagement,
            Likes: totalLikes,
            IsSimulated: true
        );
    }
}