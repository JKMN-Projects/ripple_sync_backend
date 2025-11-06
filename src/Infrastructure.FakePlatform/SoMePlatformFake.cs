using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.FakePlatform;
public class FakePlatformOptions
{
    [Required]
    public string UrlBase { get; set; }
}


public class SoMePlatformFake(IOptions<FakePlatformOptions> options) : ISoMePlatform
{
    private FakePlatformOptions Options => options.Value;

    public string GetAuthorizationUrl(AuthorizationConfiguration authConfigs)
    {
        var urlBase = Options.UrlBase + "/api/oauth/callback";
        var queryString = new QueryString()
            .Add("state", authConfigs.State)
            .Add("code", "fake_code");
        return urlBase + queryString.ToUriComponent();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, Options.UrlBase + "/api/fakeoauth/token");
        return request;
    }

    public async Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform)
    {
        foreach (var post in FakePlatformInMemoryData.PostData)
        {
            await Task.Delay(Random.Shared.Next(80, 150));
            if (!FakePlatformInMemoryData.PostStats.TryGetValue(post.Id, out var statsForPost))
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

            if (daysAtPreviousCalculation >= post.DaysSincePosted)
                continue;

            for (int i = daysAtPreviousCalculation; i < post.DaysSincePosted; i++)
            {
                reach += Random.Shared.Next(35, 135);
                engagement += Random.Shared.Next(25, 70);
                likes += Random.Shared.Next(20, 65);
            }
            FakePlatformInMemoryData.PostStats[post.Id] = statsForPost with
            {
                Days = post.DaysSincePosted,
                Likes = likes,
                Reach = reach,
                Engagement = engagement
            };
        }

        int postCount = FakePlatformInMemoryData.PostData.Count;
        int totalReach = FakePlatformInMemoryData.PostStats.Sum(kv => kv.Value.Reach);
        int totalEngagement = FakePlatformInMemoryData.PostStats.Sum(kv => kv.Value.Engagement);
        int totalLikes = FakePlatformInMemoryData.PostStats.Sum(kv => kv.Value.Likes);

        return new PlatformStats(
            PostCount: postCount,
            Reach: totalReach,
            Engagement: totalEngagement,
            Likes: totalLikes
        );
    }

    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        int mediaCount = post.PostMedias?.Count() ?? 0;
        int delay = mediaCount > 0
            ? Random.Shared.Next(800 * mediaCount, 1500 * mediaCount)
            : Random.Shared.Next(400, 900);
        await Task.Delay(delay);
        FakePlatformInMemoryData.PostData.Add(new PostData(
            Id: post.Id,
            PostedOn: DateTime.UtcNow,
            Content: post.MessageContent,
            Media: post.PostMedias?.Select(pm => new PostDataMedia(pm.Id)) ?? []
        ));

        return post.PostEvents.First();
    }
}

public static class FakePlatformInMemoryData
{
    public static List<PostData> PostData { get; private set; } = [];
    public static Dictionary<Guid, PostStats> PostStats { get; private set; } = [];
}

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