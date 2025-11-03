using System.ComponentModel.DataAnnotations;

namespace RippleSync.API.Integrations;

public record class CreateIntegrationRequest(
    [Range(1, int.MaxValue)] int PlatformId,
    [Required] string AccessToken
);
