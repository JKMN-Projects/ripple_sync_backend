
namespace RippleSync.Application.Common.Security;

public interface IPasswordHasher
{
    byte[] GenerateSalt();
    byte[] Hash(byte[] passwordBytes, byte[] salt);
    bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash);
}
