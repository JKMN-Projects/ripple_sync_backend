using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.X;

public class ThreadsOptions
{
    [Required]
    public string AppId { get; init; }

    [Required]
    public string AppSecret { get; init; }
}
