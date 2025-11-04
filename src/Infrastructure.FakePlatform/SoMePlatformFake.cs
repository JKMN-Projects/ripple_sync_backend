using Microsoft.AspNetCore.Http;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace Infrastructure.FakePlatform;

public class SoMePlatformFake : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfigs)
    {
        var urlBase = "https://localhost:7275/api/oauth/callback";
        var queryString = new QueryString()
            .Add("state", authConfigs.State)
            .Add("code", "fake_code");
        return urlBase + queryString.ToUriComponent();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7275/api/fakeoauth/token");
        return request;
    }

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration)
    {
        int CalculatePostStat(int days, int multiplierMin, int multiplierMax)
        {
            int total = 0;
            for (int i = 0; i < days; i++)
            {
                total += Convert.ToInt32(Random.Shared.Next(multiplierMin, multiplierMax));
            }
            return total;
        }

        int postCount = FakePlatformInMemoryData.PostData.Count;
        int reach = FakePlatformInMemoryData.PostData
            .Select(pd => Convert.ToInt32((pd.PostedOn - DateTime.UtcNow).TotalDays + 1))
            .Sum(days => CalculatePostStat(days, 35, 135));
        int engagement = FakePlatformInMemoryData.PostData
            .Select(pd => Convert.ToInt32((pd.PostedOn - DateTime.UtcNow).TotalDays + 1))
            .Sum(days => CalculatePostStat(days, 25, 70));
        int likes = FakePlatformInMemoryData.PostData
            .Select(pd => Convert.ToInt32((pd.PostedOn - DateTime.UtcNow).TotalDays + 1))
            .Sum(days => CalculatePostStat(days, 20, 65));

        return Task.FromResult(new PlatformStats(
            PostCount: postCount,
            Reach: reach,
            Engagement: engagement,
            Likes: likes
        ));
    }

    public Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        FakePlatformInMemoryData.PostData.Add(new PostData(
            Id: post.Id,
            PostedOn: DateTime.UtcNow,
            Content: post.MessageContent,
            Media: post.PostMedias?.Select(pm => new PostDataMedia(pm.Id)) ?? []
        ));

        return Task.FromResult(post.PostEvents.First());
    }
}

public static class FakePlatformInMemoryData
{
    public static List<PostData> PostData { get; private set; } = [];
}

public sealed record PostData(
    Guid Id,
    string Content,
    DateTime PostedOn,
    IEnumerable<PostDataMedia> Media);

public sealed record PostDataMedia(Guid Id);