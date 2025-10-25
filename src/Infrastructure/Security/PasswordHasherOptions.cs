using System.ComponentModel.DataAnnotations;

namespace RippleSync.Infrastructure.Security;

public class PasswordHasherOptions
{
    [Range(1000, int.MaxValue)]
    public int EncryptionRounds { get; init; } = 100_000;
    [Range(8, 256)]
    public int SaltByteLength { get; init; } = 32;

    [Range(8, 256)]
    public int HashByteLength { get; init; } = 32;
}