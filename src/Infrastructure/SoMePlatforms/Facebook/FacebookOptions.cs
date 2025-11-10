using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.Facebook;

public class FacebookOptions
{
    [Required]
    public string AppId { get; init; }

    [Required]
    public string AppSecret { get; init; }
}
