using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.Instagram;

public class InstagramOptions
{
    [Required]
    public string AppId { get; init; }

    [Required]
    public string AppSecret { get; init; }
}
