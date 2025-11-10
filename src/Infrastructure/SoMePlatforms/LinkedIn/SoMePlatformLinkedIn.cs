using Infrastructure.FakePlatform;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;

internal class SoMePlatformLinkedIn(
    ILogger<SoMePlatformLinkedIn> logger,
    IOptions<LinkedInOptions> options,
    LinkedInHttpClient linkedInHttpClient) : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", options.Value.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "w_member_social profile email openid")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://www.linkedin.com/oauth/v2/authorization" + queries.ToUriComponent()).ToString();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = options.Value.ClientId,
            ["client_secret"] = options.Value.ClientSecret,
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code
        };

        return new HttpRequestMessage(HttpMethod.Post, "https://www.linkedin.com/oauth/v2/accessToken")
        {
            Content = new FormUrlEncodedContent(formData)
        };
    }

    public async Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform)
        => await PostStatGenerator.CalculateAsync(integration, publishedPostsOnPlatform);

    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id)
            ?? throw new InvalidOperationException("PostEvent not found for the given integration.");

        try
        {
            linkedInHttpClient.SetDefaultHeaders(integration.TokenType, integration.AccessToken);

            var authorUrn = await linkedInHttpClient.GetUserAuthorUrnAsync();
            var imageUrns = await UploadPostImagesAsync(authorUrn, post.PostMedia);


            var builder = new LinkedInPostBuilder()
                .SetAuthor(authorUrn)
                .SetCommentary(post.MessageContent)
                .SetVisibility("PUBLIC")
                .SetLifecycleState("PUBLISHED")
                .SetReshareDisabled(false);


            if (imageUrns.Any())
            {
                builder.AddImages(imageUrns);
            }

            var publishPayload = builder.Build();


            var postIdentifier = await linkedInHttpClient.PublishPost(publishPayload);

            if (postIdentifier == string.Empty)
                logger.LogWarning("Could not get platformIdentifier on LinkedIn for post: {PostId}", post.Id);

            postEvent.PlatformPostIdentifier = postIdentifier;
            postEvent.Status = PostStatus.Posted;
        }
        catch (Exception ex)
        {
            postEvent.Status = PostStatus.Failed;
            logger.LogError(ex, "An exception occurred while publishing post {PostId} on Linkedin", post.Id);
            throw;
        }

        return postEvent;
    }
    private async Task<List<string>> UploadPostImagesAsync(string authorUrn,
        IEnumerable<PostMedia> postMedias)
    {
        var imageUrns = new List<string>();

        if (!postMedias.Any())
            return imageUrns;

        foreach (var postMedia in postMedias)
        {
            var initResponse = await linkedInHttpClient.InitImageAsync(authorUrn);
            await linkedInHttpClient.UploadImageAsync(postMedia.ImageData, initResponse.UploadUrl);
            imageUrns.Add(initResponse.Image);
        }

        return imageUrns;
    }

}
