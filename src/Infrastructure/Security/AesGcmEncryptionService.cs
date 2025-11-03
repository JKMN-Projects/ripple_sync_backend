using RippleSync.Application.Common.Security;
using System.Security.Cryptography;
using System.Text;

namespace RippleSync.Infrastructure.Security;
public class AesGcmEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private const int _nonceSize = 12;
    private const int _tagSize = 16;

    /// <summary>
    /// Initializes encryption service with a base64-encoded 256-bit key
    /// </summary>
    public AesGcmEncryptionService(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentNullException(nameof(base64Key));

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes", nameof(base64Key));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentNullException(nameof(plaintext));

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = GenerateNonce();
        byte[] tag = new byte[_tagSize];
        byte[] ciphertext = new byte[plaintextBytes.Length];

        using var aesGcm = new AesGcm(_key, _tagSize);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        byte[] encryptedData = CombineEncryptedData(nonce, tag, ciphertext);
        return Convert.ToBase64String(encryptedData);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
            throw new ArgumentNullException(nameof(ciphertext));

        byte[] encryptedData = Convert.FromBase64String(ciphertext);
        var (nonce, tag, ciphertextBytes) = SplitEncryptedData(encryptedData);

        byte[] plaintext = new byte[ciphertextBytes.Length];

        using var aesGcm = new AesGcm(_key, _tagSize);
        aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GenerateNonce()
    {
        byte[] nonce = new byte[_nonceSize];
        RandomNumberGenerator.Fill(nonce);
        return nonce;
    }

    private byte[] CombineEncryptedData(byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        byte[] result = new byte[nonce.Length + tag.Length + ciphertext.Length];

        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        ciphertext.CopyTo(result, nonce.Length + tag.Length);

        return result;
    }

    private (byte[] nonce, byte[] tag, byte[] ciphertext) SplitEncryptedData(byte[] encryptedData)
    {
        byte[] nonce = new byte[_nonceSize];
        byte[] tag = new byte[_tagSize];
        byte[] ciphertext = new byte[encryptedData.Length - _nonceSize - _tagSize];

        Array.Copy(encryptedData, 0, nonce, 0, _nonceSize);
        Array.Copy(encryptedData, _nonceSize, tag, 0, _tagSize);
        Array.Copy(encryptedData, _nonceSize + _tagSize, ciphertext, 0, ciphertext.Length);

        return (nonce, tag, ciphertext);
    }
}
