using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.Facebook;

public class FacebookOptions
{
    [Required]
    public required string AppId { get; init; }

    [Required]
    public required string AppSecret { get; init; }
}
