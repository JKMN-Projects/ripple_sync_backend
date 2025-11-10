using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn.Models;

internal record InitMedia(
    [property: JsonPropertyName("value")] LinkedInMediaInitResponse Value);
