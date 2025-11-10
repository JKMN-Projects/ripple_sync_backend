using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

public class XOptions
{
    [Required]
    public required string ClientId { get; init; }

    [Required]
    public required string ClientSecret { get; init; }
}
