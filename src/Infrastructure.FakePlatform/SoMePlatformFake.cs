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
    public required string UrlBase { get; init; }
}


public class SoMePlatformFake(
    IOptions<FakePlatformOptions> options) : ISoMePlatform
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
        => await PostStatGenerator.CalculateAsync(integration, publishedPostsOnPlatform);

    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        int mediaCount = post.PostMedia?.Count() ?? 0;
        int delay = mediaCount > 0
            ? Random.Shared.Next(800 * mediaCount, 1500 * mediaCount)
            : Random.Shared.Next(400, 900);
        await Task.Delay(delay);
        FakePlatformInMemoryData.PostData.Add(new PostData(
            Id: post.Id,
            PostedOn: DateTime.UtcNow,
            Content: post.MessageContent,
            Media: post.PostMedia?.Select(pm => new PostDataMedia(pm.Id)) ?? []
        ));

        return post.PostEvents.First(pe => pe.UserPlatformIntegrationId == integration.Id);
    }
}
