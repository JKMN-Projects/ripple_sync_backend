using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn.Models;

internal class LinkedInUserInfo
{
    [JsonPropertyName("sub")]
    public string Sub { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}
