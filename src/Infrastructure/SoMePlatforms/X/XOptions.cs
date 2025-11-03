using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

public class XOptions
{
    [Required]
    public string ClientId { get; init; }

    [Required]
    public string ClientSecret { get; init; }
}
