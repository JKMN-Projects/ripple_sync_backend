using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.Security;

public sealed class JwtOptions
{
    [Required]
    public required string Key { get; init; }

    [Required]
    public required string Url { get; init; }

    [Required]
    public required int ValidityInMinutes { get; init; }

    [Required]
    public required int RefreshTokenValidityInDays { get; init; }
}