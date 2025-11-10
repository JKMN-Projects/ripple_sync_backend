using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.Instagram;

public class InstagramOptions
{
    [Required]
    public required string AppId { get; init; }

    [Required]
    public required string AppSecret { get; init; }
}
