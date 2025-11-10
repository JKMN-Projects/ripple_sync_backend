using RippleSync.Infrastructure.Security;
using RippleSync.Tests.Common.TestDoubles.Options;
using System.Security.Cryptography;
using System.Text;

namespace RippleSync.Infrastructure.Tests.Security;

public class Rfc2898PasswordHasherTests
{

    protected static Rfc2898PasswordHasher GetSystemUnderTest(
        PasswordHasherOptions? passwordHasherOptions = null)
    {
        passwordHasherOptions ??= new();
        var optionsMonitor = new OptionsMonitorDoubles.Stubs.FixedOptionsMonitor<PasswordHasherOptions>(passwordHasherOptions);
        return new Rfc2898PasswordHasher(optionsMonitor);
    }

    public sealed class Verify : Rfc2898PasswordHasherTests
    {
        [Fact]
        public void Should_ReturnTrue_WhenPasswordHashMatchesPasswordAndSalt()
        {
            // Arrange
            Rfc2898PasswordHasher hasher = GetSystemUnderTest();
            string password = "Password123!";
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] salt = RandomNumberGenerator.GetBytes(32);
            byte[] passwordHash = hasher.Hash(passwordBytes, salt);

            // Act
            bool isValid = hasher.Verify(passwordBytes, salt, passwordHash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void Should_ReturnFalse_WhenPasswordHashDoesNotMatchPasswordAndSalt()
        {
            // Arrange
            Rfc2898PasswordHasher hasher = GetSystemUnderTest();
            byte[] salt = RandomNumberGenerator.GetBytes(32);
            byte[] passwordBytes = Encoding.UTF8.GetBytes("Password123!");
            byte[] passwordHash = hasher.Hash(passwordBytes, salt);
            byte[] otherPasswordBytes = Encoding.UTF8.GetBytes("WrongPassword123!");

            // Act
            bool isValid = hasher.Verify(otherPasswordBytes, salt, passwordHash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Should_ReturnFalse_WhenSaltIsDifferent()
        {
            // Arrange
            Rfc2898PasswordHasher hasher = GetSystemUnderTest();
            byte[] salt = RandomNumberGenerator.GetBytes(32);
            byte[] passwordBytes = Encoding.UTF8.GetBytes("Password123!");
            byte[] passwordHash = hasher.Hash(passwordBytes, salt);
            byte[] differentSalt = RandomNumberGenerator.GetBytes(32);

            // Act
            bool isValid = hasher.Verify(passwordBytes, differentSalt, passwordHash);

            // Assert
            Assert.False(isValid);
        }
    }
}
