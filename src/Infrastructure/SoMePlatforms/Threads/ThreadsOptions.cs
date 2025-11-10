using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.SoMePlatforms.Threads;

public class ThreadsOptions
{
    [Required]
    public required string AppId { get; init; }

    [Required]
    public required string AppSecret { get; init; }
}
