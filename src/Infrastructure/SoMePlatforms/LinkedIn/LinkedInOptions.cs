using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

public class LinkedInOptions
{
    [Required]
    public string ClientId { get; init; }

    [Required]
    public string ClientSecret { get; init; }
}
