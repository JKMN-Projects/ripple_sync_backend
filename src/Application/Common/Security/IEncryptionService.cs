namespace RippleSync.Application.Common.Security;
public interface IEncryptionService
{
    /// <summary>
    /// New encryption value for same values. 
    /// More secure, but disables where clauses for these values
    /// </summary>
    /// <param name="task"></param>
    /// <param name="plaintext"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    string Encrypt(EncryptionTask task, string plaintext);

    /// <summary>
    /// Same encryption value for same values. 
    /// Less secure, but enables where clauses for these values
    /// </summary>
    /// <param name="task"></param>
    /// <param name="plaintext"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    string EncryptDeterministic(EncryptionTask task, string plaintext);

    /// <summary>
    /// Decrypt both normal and deterministic, based on shared nonce value.
    /// </summary>
    /// <param name="task"></param>
    /// <param name="ciphertext"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    string Decrypt(EncryptionTask task, string ciphertext);
}
