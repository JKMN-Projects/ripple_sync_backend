
using RippleSync.Application.Common.Security;
using System.Security.Cryptography;
using System.Text;

namespace RippleSync.Tests.Common.TestDoubles.Security;

public static partial class PasswordHasherDoubles
{
    public class Dummy : IPasswordHasher
    {
        public virtual byte[] GenerateSalt()
            => throw new NotImplementedException();

        public virtual byte[] Hash(byte[] passwordBytes, byte[] salt)
            => throw new NotImplementedException();

        public virtual bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
            => throw new NotImplementedException();
    }

    public static class Stubs
    {
        public static class Verify
        {
            public class AlwaysValid : Dummy
            {
                public override bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
                    => true;
            }

            public class AlwaysInvalid : Dummy
            {
                public override bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
                    => false;
            }
        }
    }

    public static class Spies
    {
        public sealed class VerifySpy : Dummy
        {
            public byte[]? LastReceivedPasswordBytes { get; private set; }
            public byte[]? LastReceivedSalt { get; private set; }
            public byte[]? LastReceivedHash { get; private set; }
            public int InvokationCount { get; private set; }

            private readonly IPasswordHasher spiedHasher;

            public VerifySpy(IPasswordHasher spiedHasher)
            {
                this.spiedHasher = spiedHasher;
            }

            public override bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
            {
                LastReceivedPasswordBytes = passwordBytes;
                LastReceivedSalt = salt;
                LastReceivedHash = hash;
                InvokationCount++;
                return spiedHasher.Verify(passwordBytes, salt, hash);
            }
        }
    }

    public static class Fakes
    {
        public class Base64Hasher : IPasswordHasher
        {
            public byte[] GenerateSalt()
            {
                using var randomNumberGenerator = RandomNumberGenerator.Create();

                byte[] randomBytes = new byte[32];
                randomNumberGenerator.GetBytes(randomBytes);
                return randomBytes;
            }

            public byte[] Hash(byte[] passwordBytes, byte[] salt)
                => Encoding.UTF8.GetBytes(Convert.ToBase64String(passwordBytes) + Convert.ToBase64String(salt));

            public bool Verify(byte[] passwordBytes, byte[] salt, byte[] hash)
            {
                byte[] computedHash = Hash(passwordBytes, salt);
                return computedHash.SequenceEqual(hash);
            }
        }
    }
}
