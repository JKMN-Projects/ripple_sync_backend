using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.SoMePlatforms.X;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;

internal class SoMePlatformLinkedIn(IOptions<LinkedInOptions> options, IEncryptionService encryptor) : ISoMePlatform
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

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform)
    {
        return Task.FromResult(new PlatformStats(
            PostCount: 0,
            Reach: 0,
            Engagement: 0,
            Likes: 0
        ));
    }
    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id)
            ?? throw new InvalidOperationException("PostEvent not found for the given integration.");
        var authorUrn = await GetLinkedInAuthorUrnAsync(integration);
        //TODO: Implement LinkedIn post publishing
        var url = "https://api.linkedin.com/rest/posts";
        var linkedInPayload = new
        {
            author = authorUrn,
            commentary = post.MessageContent,
            visibility = "PUBLIC",
            distribution = new
            {
                feedDistribution = "MAIN_FEED",
                targetEntities = new List<string>(),
                thirdPartyDistributionChannels = new List<string>()
            },
            lifecycleState = "PUBLISHED",
            isReshareDisabledByAuthor = false
        };
        var jsonContent = JsonSerializer.Serialize(linkedInPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");


        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        request.Headers.Add("Linkedin-Version", "202510");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            encryptor.Decrypt(integration.AccessToken)
        );

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            postEvent.Status = PostStatus.Posted;
        }
        else
        {
            postEvent.Status = PostStatus.Failed;
        }
        return postEvent;

    }
    private async Task<string> GetLinkedInAuthorUrnAsync(Integration integration)
    {
        var userInfoUrl = "https://api.linkedin.com/v2/userinfo";

        var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            encryptor.Decrypt(integration.AccessToken)
        );

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to retrieve LinkedIn user info: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<LinkedInUserInfo>(responseContent);

        return $"urn:li:person:{userInfo!.Sub}";
    }

    public class LinkedInUserInfo
    {
        [JsonPropertyName("sub")]
        public string Sub { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }
    }
}
