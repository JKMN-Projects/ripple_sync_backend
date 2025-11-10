using System.Text.Json.Serialization;

namespace RippleSync.Infrastructure.SoMePlatforms.LinkedIn.Models;

internal record LinkedInMediaInitResponse(
    [property: JsonPropertyName("uploadUrl")] string UploadUrl,
    [property: JsonPropertyName("image")] string Image);
