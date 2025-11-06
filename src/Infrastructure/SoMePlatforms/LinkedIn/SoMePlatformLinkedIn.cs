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
using System.Text.Json.Nodes;
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

    public Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration)
    {
        return Task.FromResult(new PlatformStats(
            PostCount: 0,
            Reach: 0,
            Engagement: 0,
            Likes: 0
        ));
    }
    private class PublishPayload()
    {
        [JsonPropertyName("author")]
        public string Author { get; set; }
        [JsonPropertyName("commentary")]
        public string Commentary { get; set; }
        [JsonPropertyName("visibility")]
        public string Visibility { get; set; } = "PUBLIC";
        [JsonPropertyName("lifecycleState")]
        public string LifecycleState { get; set; } = "PUBLISHED";
        [JsonPropertyName("isReshareDisabledByAuthor")]
        public bool IsReshareDisabledByAuthor { get; set; } = false;
        [JsonPropertyName("distribution")]
        public Distribution Distribution { get; set; } = new Distribution();
        [JsonPropertyName("content")]
        public Content Content { get; set; }

    }
    private partial class Distribution
    {
        [JsonPropertyName("feedDistribution")]
        public string FeedDistribution { get; set; } = "MAIN_FEED";
        [JsonPropertyName("targetEntities")]
        public List<string> TargetEntities { get; set; } = new List<string>();
        [JsonPropertyName("thirdPartyDistributionChannels")]
        public List<string> ThirdPartyDistributionChannels { get; set; } = new List<string>();
    }

    private partial class Content
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("multiImage")]
        public MultiImage MultiImage { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("media")]
        public Media Media { get; set; }
    }

    private partial class MultiImage
    {
        [JsonPropertyName("images")]
        public List<Media> Images { get; set; }
    }
    private partial class Media
    {
        [JsonPropertyName("altText")]
        public string AltText { get; set; }
        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id)
            ?? throw new InvalidOperationException("PostEvent not found for the given integration.");
        var authorUrn = await GetLinkedInAuthorUrnAsync(integration);
        var url = "https://api.linkedin.com/rest/posts";

        List<string> imageUrns = new List<string>();
        if (post.PostMedias.Any())
        {
            foreach (var postMedia in post.PostMedias)
            {
                var initResponse = await InitImage(integration);
                await UploadImage(integration, postMedia.ImageData, initResponse.uploadUrl);
                imageUrns.Add(initResponse.image);
            }
        }

        //TODO: Create Payload class model
        var publishPayload = new PublishPayload()
        {
            Author = authorUrn,
            Commentary = post.MessageContent,
            Visibility = "PUBLIC",
            LifecycleState = "PUBLISHED",
            IsReshareDisabledByAuthor = false,
            Distribution = new Distribution()
            {
                FeedDistribution = "MAIN_FEED",
                TargetEntities = new List<string>(),
                ThirdPartyDistributionChannels = new List<string>()
            }
        };

        //If single image append to media
        if (imageUrns.Count == 1)
        {
            publishPayload.Content = new Content()
            {
                Media = new Media()
                {
                    AltText = "Uploaded Image",
                    Id = imageUrns.First()
                }
            };
        }
        //If mutliple images append to multi image
        if (imageUrns.Count > 1)
        {
            publishPayload.Content = new Content()
            {
                MultiImage = new MultiImage()
                {
                    Images = imageUrns.Select(urn => new Media()
                    {
                        AltText = "Uploaded Image",
                        Id = urn
                    }).ToList()
                }
            };

        }


        //var linkedInPayload = new
        //{
        //    author = authorUrn,
        //    commentary = post.MessageContent,
        //    visibility = "PUBLIC",
        //    distribution = new
        //    {
        //        feedDistribution = "MAIN_FEED",
        //        targetEntities = new List<string>(),
        //        thirdPartyDistributionChannels = new List<string>()
        //    },
        //    lifecycleState = "PUBLISHED",
        //    isReshareDisabledByAuthor = false
        //};

        var jsonContent = JsonSerializer.Serialize(publishPayload);
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
    private async Task UploadImage(Integration integration, string base64Img, string url)
    {

        using var httpClient = new HttpClient();

        // Set authorization header
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", encryptor.Decrypt(integration.AccessToken));
        
        var imageBytes = Convert.FromBase64String(base64Img);
        using var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        content.Headers.Add("Linkedin-Version", "202510");
        var response = await httpClient.PutAsync(url, content);
        var responseBody = await response.Content.ReadAsStringAsync();
    }
    private async Task<LinkedInMediaInitResponse> InitImage(Integration integration)
    {
        var initImagePayload = new
        {
            initializeUploadRequest = new
            {
                owner = await GetLinkedInAuthorUrnAsync(integration)
            }
        };

        var jsonContent = JsonSerializer.Serialize(initImagePayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.linkedin.com/rest/images?action=initializeUpload")
        {
            Content = content
        };
        request.Headers.Add("X-Restli-Protocol-Version", "2.0.0");
        request.Headers.Add("Linkedin-Version", "202510");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            integration.TokenType,
            encryptor.Decrypt(integration.AccessToken)
        );

        using var httpClient = new HttpClient();
        var intitMediaContent = await httpClient.SendAsync(request);
        var intitMediaContentResponse = await intitMediaContent.Content.ReadAsStringAsync();
        InitMedia mediaResponse = null;
        if (intitMediaContent.IsSuccessStatusCode)
        {
            mediaResponse = JsonSerializer.Deserialize<InitMedia>(intitMediaContentResponse);
        }
        return mediaResponse!.value;
    }
    record InitMedia(LinkedInMediaInitResponse value);
    record LinkedInMediaInitResponse(string uploadUrl, string image);
}
