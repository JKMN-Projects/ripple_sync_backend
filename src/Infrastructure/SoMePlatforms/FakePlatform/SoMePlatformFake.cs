using Microsoft.AspNetCore.Http;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Infrastructure.SoMePlatforms.FakePlatform;

internal class SoMePlatformFake : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfigs)
    {
        string urlBase = "https://localhost:7275/api/oauth/callback";
        QueryString queryString = new QueryString()
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
        => throw new NotImplementedException();

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