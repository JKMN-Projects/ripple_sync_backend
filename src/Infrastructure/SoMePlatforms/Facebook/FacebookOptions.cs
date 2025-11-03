using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

public class FacebookOptions
{
    [Required]
    public string AppId { get; init; }

    [Required]
    public string AppSecret { get; init; }
}
