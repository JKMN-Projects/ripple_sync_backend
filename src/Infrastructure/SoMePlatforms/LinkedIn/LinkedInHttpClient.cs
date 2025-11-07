using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn;

internal class LinkedInHttpClient(HttpClient httpClient)
{
    internal void SetDefaultHeaders(string? tokenType, string accessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            tokenType ?? "Bearer",
            accessToken);
    }
    internal async Task<string> PublishPost(PublishPayload publishPayload)
    {
        const string url = "rest/posts";

        var jsonContent = JsonSerializer.Serialize(publishPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to publish post to LinkedIn. Status: {response.StatusCode}, Response: {responseContent}");
        }
        else
        {
            if (response.Headers.TryGetValues("x-restli-id", out var values))
            {
                return values.First();
            }
            else
            {
                return "";
            }
        }
    }
    internal async Task<string> GetUserAuthorUrnAsync()
    {
        const string userInfoUrl = "v2/userinfo";

        var request = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);

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
    internal async Task<LinkedInMediaInitResponse> InitImageAsync(string authorUrn)
    {

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
            "rest/images?action=initializeUpload")
        {
            Content = content
        };
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
    internal async Task UploadImageAsync(string base64Img, string uploadUrl)
    {
        var imageBytes = Convert.FromBase64String(base64Img);
        using var content = new ByteArrayContent(imageBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var request = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };


        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Failed to upload image. Status: {response.StatusCode}, Response: {responseBody}");
        }
    }
}
internal record InitMedia(LinkedInMediaInitResponse value);
internal record LinkedInMediaInitResponse(string uploadUrl, string image);
internal class LinkedInUserInfo
{
    [JsonPropertyName("sub")]
    public string Sub { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }
}
