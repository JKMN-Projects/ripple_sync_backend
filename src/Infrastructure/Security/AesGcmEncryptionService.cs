using Microsoft.Extensions.Configuration;
using RippleSync.Application.Common.Security;
using System.Security.Cryptography;
using System.Text;

namespace RippleSync.Infrastructure.Security;
public class AesGcmEncryptionService : IEncryptionService
{
    private readonly byte[] _integrationAccessTokenKey;
    private readonly byte[] _integrationRefreshTokenKey;
    private readonly byte[] _postMessageKey;
    private readonly byte[] _postMediaKey;
    private readonly byte[] _userEmailKey;
    private readonly byte[] _userTokenValueKey;

    private const int _nonceSize = 12;
    private const int _tagSize = 16;

    /// <summary>
    /// Initializes encryption service with a base64-encoded 256-bit key
    /// </summary>
    public AesGcmEncryptionService(IConfiguration configs)
    {
        string integrationAccessTokenKey = configs["Encryption:IntegrationAccessTokenKey"]
                ?? throw new ArgumentException($"{nameof(integrationAccessTokenKey)} empty");

        string integrationRefreshTokenKey = configs["Encryption:IntegrationRefreshTokenKey"]
                ?? throw new ArgumentException($"{nameof(integrationRefreshTokenKey)} empty");

        string postMessageKey = configs["Encryption:PostMessageKey"]
                ?? throw new ArgumentException($"{nameof(postMessageKey)} empty");

        string postMediaKey = configs["Encryption:PostMediaKey"]
                ?? throw new ArgumentException($"{nameof(postMediaKey)} empty");

        string userEmailKey = configs["Encryption:UserEmailKey"]
                ?? throw new ArgumentException($"{nameof(userEmailKey)} empty");

        string userTokenValueKey = configs["Encryption:UserTokenValueKey"]
               ?? throw new ArgumentException($"{nameof(userTokenValueKey)} empty");

        _integrationAccessTokenKey = Convert.FromBase64String(integrationAccessTokenKey);
        _integrationRefreshTokenKey = Convert.FromBase64String(integrationRefreshTokenKey);
        _postMessageKey = Convert.FromBase64String(postMessageKey);
        _postMediaKey = Convert.FromBase64String(postMediaKey);
        _userEmailKey = Convert.FromBase64String(userEmailKey);
        _userTokenValueKey = Convert.FromBase64String(userTokenValueKey);

        if (_integrationAccessTokenKey.Length != 32)
            throw new InvalidDataException($"{nameof(_integrationAccessTokenKey)} must be 32 bytes");

        if (_integrationRefreshTokenKey.Length != 32)
            throw new InvalidDataException($"{nameof(_integrationRefreshTokenKey)} must be 32 bytes");

        if (_postMessageKey.Length != 32)
            throw new InvalidDataException($"{nameof(_postMessageKey)} must be 32 bytes");

        if (_postMediaKey.Length != 32)
            throw new InvalidDataException($"{nameof(_postMediaKey)} must be 32 bytes");

        if (_userEmailKey.Length != 32)
            throw new InvalidDataException($"{nameof(_userEmailKey)} must be 32 bytes");

        if (_userTokenValueKey.Length != 32)
            throw new InvalidDataException($"{nameof(_userTokenValueKey)} must be 32 bytes");
    }

    public string Encrypt(EncryptionTask task, string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentNullException(nameof(plaintext));

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = GenerateNonce();
        byte[] tag = new byte[_tagSize];
        byte[] ciphertext = new byte[plaintextBytes.Length];

        switch (task)
        {
            case EncryptionTask.IntegrationAccessToken:
                {
                    using var aesGcm = new AesGcm(_integrationAccessTokenKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.IntegrationRefreshToken:
                {
                    using var aesGcm = new AesGcm(_integrationRefreshTokenKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.PostMessageContent:
                {
                    using var aesGcm = new AesGcm(_postMessageKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.PostMediaContent:
                {
                    using var aesGcm = new AesGcm(_postMediaKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.UserEmail:
            case EncryptionTask.UserTokenValue:
            default:
                throw new InvalidOperationException($"Encryption task not supported for {task}");
        }

        byte[] encryptedData = CombineEncryptedData(nonce, tag, ciphertext);
        return Convert.ToBase64String(encryptedData);
    }


    public string EncryptDeterministic(EncryptionTask task, string plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
            throw new ArgumentNullException(nameof(plaintext));

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = GenerateDeterministicNonce(plaintext);
        byte[] tag = new byte[_tagSize];
        byte[] ciphertext = new byte[plaintextBytes.Length];

        switch (task)
        {
            case EncryptionTask.UserEmail:
                {
                    using var aesGcm = new AesGcm(_userEmailKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.UserTokenValue:
                {
                    using var aesGcm = new AesGcm(_userTokenValueKey, _tagSize);
                    aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
                }
                break;
            case EncryptionTask.IntegrationAccessToken:
            case EncryptionTask.IntegrationRefreshToken:
            case EncryptionTask.PostMessageContent:
            case EncryptionTask.PostMediaContent:
            default:
                throw new InvalidOperationException($"Deterministic encryption task not supported for {task}");
        }

        byte[] encryptedData = CombineEncryptedData(nonce, tag, ciphertext);
        return Convert.ToBase64String(encryptedData);
    }

    public string Decrypt(EncryptionTask task, string ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
            throw new ArgumentNullException(nameof(ciphertext));

        byte[] encryptedData = Convert.FromBase64String(ciphertext);
        var (nonce, tag, ciphertextBytes) = SplitEncryptedData(encryptedData);

        byte[] plaintext = new byte[ciphertextBytes.Length];

        switch (task)
        {
            case EncryptionTask.IntegrationAccessToken:
                {
                    using var aesGcm = new AesGcm(_integrationAccessTokenKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            case EncryptionTask.IntegrationRefreshToken:
                {
                    using var aesGcm = new AesGcm(_integrationRefreshTokenKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            case EncryptionTask.PostMessageContent:
                {
                    using var aesGcm = new AesGcm(_postMessageKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            case EncryptionTask.PostMediaContent:
                {
                    using var aesGcm = new AesGcm(_postMediaKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            case EncryptionTask.UserEmail:
                {
                    using var aesGcm = new AesGcm(_userEmailKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            case EncryptionTask.UserTokenValue:
                {
                    using var aesGcm = new AesGcm(_userTokenValueKey, _tagSize);
                    aesGcm.Decrypt(nonce, ciphertextBytes, tag, plaintext);
                }
                break;
            default:
                throw new InvalidOperationException("Encryption task not supported");
        }

        return Encoding.UTF8.GetString(plaintext);
    }


    private byte[] GenerateDeterministicNonce(string plaintext)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(plaintext));
        byte[] nonce = new byte[_nonceSize];
        Array.Copy(hash, nonce, _nonceSize);
        return nonce;
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
