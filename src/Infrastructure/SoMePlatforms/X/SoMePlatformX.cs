using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;
using System.Text;
using System.Text.Json;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

internal class SoMePlatformX(IOptions<XOptions> options, IEncryptionService encryptor) : ISoMePlatform
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
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        return request;
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


            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                integration.TokenType,
                encryptor.Decrypt(integration.AccessToken)
            );

            using var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                //TODO: Save url for the post
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
}
