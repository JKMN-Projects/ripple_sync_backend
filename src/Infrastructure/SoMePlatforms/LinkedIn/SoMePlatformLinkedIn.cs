using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.SoMePlatforms.X;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;

internal partial class SoMePlatformLinkedIn(IOptions<LinkedInOptions> options, IEncryptionService encryptor) : ISoMePlatform
{
    private const string LinkedInApiVersion = "202510";
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

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration)
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

        try
        {
            var authorUrn = await GetLinkedInAuthorUrnAsync(integration);
            var imageUrns = await UploadPostImagesAsync(post.PostMedias, integration);

            
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

           
            await PublishToLinkedInAsync(publishPayload, integration);

            postEvent.Status = PostStatus.Posted;
        }
        catch (Exception ex)
        {
            postEvent.Status = PostStatus.Failed;
            throw;
        }

        return postEvent;
    }
    private async Task<List<string>> UploadPostImagesAsync(
        IEnumerable<PostMedia> postMedias,
        Integration integration)
    {
        var imageUrns = new List<string>();

        if (!postMedias.Any())
            return imageUrns;

        foreach (var postMedia in postMedias)
        {
            var initResponse = await InitializeImageUploadAsync(integration);
            await UploadImageAsync(integration, postMedia.ImageData, initResponse.uploadUrl);
            imageUrns.Add(initResponse.image);
        }

        return imageUrns;
    }

    private async Task PublishToLinkedInAsync(PublishPayload payload, Integration integration)
    {
        const string url = "https://api.linkedin.com/rest/posts";

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        AddLinkedInHeaders(request, integration);

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to publish post to LinkedIn. Status: {response.StatusCode}, Response: {responseContent}");
        }
    }

    private async Task<string> GetLinkedInAuthorUrnAsync(Integration integration)
    {
        const string userInfoUrl = "https://api.linkedin.com/v2/userinfo";

        var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
        AddLinkedInHeaders(request, integration);

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to retrieve LinkedIn user info: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<LinkedInUserInfo>(responseContent);

        return $"urn:li:person:{userInfo!.Sub}";
    }

    private async Task<LinkedInMediaInitResponse> InitializeImageUploadAsync(Integration integration)
    {
        var authorUrn = await GetLinkedInAuthorUrnAsync(integration);

        var initImagePayload = new
        {
            initializeUploadRequest = new
            {
                owner = authorUrn
            }
        };

        var jsonContent = JsonSerializer.Serialize(initImagePayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.linkedin.com/rest/images?action=initializeUpload")
        {
            Content = content
        };

        AddLinkedInHeaders(request, integration);

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to initialize image upload. Status: {response.StatusCode}, Response: {responseContent}");
        }

        var mediaResponse = JsonSerializer.Deserialize<InitMedia>(responseContent);
        return mediaResponse!.value;
    }

    private async Task UploadImageAsync(Integration integration, string base64Img, string uploadUrl)
    {
        var imageBytes = Convert.FromBase64String(base64Img);
        using var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };

        AddLinkedInHeaders(request, integration);

        using var httpClient = new HttpClient();
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to upload image. Status: {response.StatusCode}, Response: {responseBody}");
        }
    }

    private void AddLinkedInHeaders(HttpRequestMessage request, Integration integration)
    {
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        request.Headers.Add("Linkedin-Version", LinkedInApiVersion);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            integration.TokenType ?? "Bearer",
            encryptor.Decrypt(integration.AccessToken)
        );
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
    record InitMedia(LinkedInMediaInitResponse value);
    record LinkedInMediaInitResponse(string uploadUrl, string image);
}
