
using Microsoft.Extensions.Options;
using RippleSync.Application.Common.Security;
using System.Security.Cryptography;

namespace RippleSync.Infrastructure.Security;

public sealed class Rfc2898PasswordHasher : IPasswordHasher
{
    private readonly IOptionsMonitor<PasswordHasherOptions> optionsMonitor;

    private PasswordHasherOptions Options => optionsMonitor.CurrentValue;

    public Rfc2898PasswordHasher(IOptionsMonitor<PasswordHasherOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
    }

    public byte[] GenerateSalt()
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

        byte[] randomBytes = new byte[Options.SaltByteLength];
        randomNumberGenerator.GetBytes(randomBytes);
        return randomBytes;
    }

    public byte[] Hash(byte[] passwordBytes, byte[] salt)
    {
        if (passwordBytes.Length == 0) throw new ArgumentException("Byte array was empty.", nameof(passwordBytes));
        if (salt.Length == 0) throw new ArgumentException("Byte array was empty.", nameof(salt));

        using Rfc2898DeriveBytes rfc2898 = new(passwordBytes, salt, Options.EncryptionRounds, HashAlgorithmName.SHA256);
        return rfc2898.GetBytes(Options.HashByteLength);
    }

    public bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
    {
        if (passwordBytes.Length == 0) throw new ArgumentException("Byte array was empty.", nameof(passwordBytes));
        if (salt.Length == 0) throw new ArgumentException("Byte array was empty.", nameof(salt));
        if (hash.Length == 0) throw new ArgumentException("Byte array was empty.", nameof(hash));

        byte[] computedHash = Hash(passwordBytes, salt);
        return CryptographicOperations.FixedTimeEquals(computedHash, hash);
    }
}
