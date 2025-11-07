using Infrastructure.FakePlatform;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using RippleSync.Application.Common.Responses;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Posts;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

internal class SoMePlatformX(
    ILogger<SoMePlatformX> logger,
    IOptions<XOptions> options,
    IEncryptionService encryptor) : ISoMePlatform
{
    public string GetAuthorizationUrl(AuthorizationConfiguration authConfig)
    {
        var queries = new QueryString()
            .Add("response_type", "code")
            .Add("client_id", options.Value.ClientId)
            .Add("redirect_uri", authConfig.RedirectUri)
            .Add("scope", "tweet.read+tweet.write+users.read+offline.access")
            .Add("state", authConfig.State)
            .Add("code_challenge", authConfig.CodeChallenge)
            .Add("code_challenge_method", "S256");

        return new Uri("https://x.com/i/oauth2/authorize" + queries.ToUriComponent()).ToString();
    }

    public HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs)
    {
        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = tokenConfigs.RedirectUri,
            ["code"] = tokenConfigs.Code,
            ["code_verifier"] = tokenConfigs.CodeVerifier
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/2/oauth2/token")
        {
            Content = new FormUrlEncodedContent(formData)
        };

        // Basic Auth header
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Value.ClientId}:{options.Value.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return request;
    }

    public async Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform)
    {
        if (integration.Platform != Platform.X)
            throw new ArgumentException($"Integration is for another platform. Expected Platform 'X'. Found '{integration.Platform}'.", nameof(integration));

        var authHeader = new AuthenticationHeaderValue(
            integration.TokenType,
            encryptor.Decrypt(integration.AccessToken)
        );
        using var httpClient = new HttpClient();
        IEnumerable<string> postIds = publishedPostsOnPlatform
            .Where(post => post.PostEvents.Any(pe => pe.UserPlatformIntegrationId == integration.Id))
            .Select(post => post.PostEvents.First(pe => pe.UserPlatformIntegrationId == integration.Id))
            .Where(postEvent => postEvent?.Status == PostStatus.Posted)
            .Where(postEvent => !string.IsNullOrEmpty(postEvent?.PlatformPostIdentifier))
            .Select(postEvent => postEvent?.PlatformPostIdentifier!);

        List<string> includedFields = [
                "public_metrics",
                "organic_metrics",
                "non_public_metrics"
            ];
        QueryString queries = new QueryString()
            .Add("ids", string.Join(',', postIds))
            .Add("tweet.fields", string.Join(',', includedFields));

        int postCount = 0;
        int likes = 0;
        int engagements = 0;

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.x.com/2/tweets" + queries.ToUriComponent());
        request.Headers.Authorization = authHeader;
        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string responseContent = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Failed to retrieve insights for Integration ID {IntegrationId}. Response Status: {StatusCode} - {ResponseContent}", 
                integration.Id, response.StatusCode, responseContent);
            return response.StatusCode == HttpStatusCode.TooManyRequests
                ? await PostStatGenerator.CalculateAsync(integration, publishedPostsOnPlatform)
                : PlatformStats.Empty;
        }
        var jsonObject = await response.Content.ReadFromJsonAsync<JsonObject>();
        if (jsonObject == null)
        {
            logger.LogWarning("Received null JSON response for Integration ID {IntegrationId}.", integration.Id);
            return PlatformStats.Empty;
        }
        JsonArray? dataArray = jsonObject["data"]?.AsArray();
        if (dataArray == null)
        {
            logger.LogWarning("No data found in JSON response for Integration ID {IntegrationId}.", integration.Id);
            return PlatformStats.Empty;
        }

        foreach (JsonNode? data in dataArray)
        {
            JsonObject tweet = data!.AsObject();
            likes += tweet["public_metrics"]?["like_count"]?.GetValue<int>() ?? 0;
            engagements += tweet["non_public_metrics"]?["engagements"]?.GetValue<int>() ?? 0;
            postCount++;
        }

        return new PlatformStats(
            PostCount: postCount,
            Reach: 0,
            Engagement: engagements,
            Likes: likes
        );
    }

    public async Task<PostEvent> PublishPostAsync(Post post, Integration integration)
    {
        var postEvent = post.PostEvents.FirstOrDefault(pe => pe.UserPlatformIntegrationId == integration.Id)
            ?? throw new InvalidOperationException("PostEvent not found for the given integration.");
        try
        {
            var tweetPayload = new
            {
                text = post.MessageContent
            };

            var jsonContent = JsonSerializer.Serialize(tweetPayload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");


            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.com/2/tweets")
            {
                Content = content
            };

            request.Headers.Authorization = new AuthenticationHeaderValue(
                integration.TokenType,
                encryptor.Decrypt(integration.AccessToken)
            );

            using var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var postResponse = JsonSerializer.Deserialize<PostResponse>(responseContent);
                postEvent.PlatformPostIdentifier = postResponse?.Data?.Id;
                if (postEvent.PlatformPostIdentifier == null) logger.LogWarning("Platform Identification could not be found");
                postEvent.Status = PostStatus.Posted;
            }
            else
            {
                throw new InvalidOperationException($"Failed to publish post to X. Response: {responseContent}");
            }
        }
        catch (Exception e)
        {
            postEvent.Status = PostStatus.Failed;
            throw;
        }
        return postEvent;
    }
    private record PostResponse(PostResponseData Data);
    private record PostResponseData(string Id);
   
}
