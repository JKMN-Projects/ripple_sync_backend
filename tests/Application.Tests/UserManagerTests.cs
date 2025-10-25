
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Users;
using RippleSync.Domain.Users;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Logging;
using RippleSync.Tests.Shared.TestDoubles.Repositories;
using RippleSync.Tests.Shared.TestDoubles.Security;
using System.Text;

namespace RippleSync.Application.Tests;

public abstract class UserManagerTests
{
    protected static UserManager GetSystemUnderTest(
        IUserRepository? userRepository = null,
        IPasswordHasher? passwordHasher = null,
        IAuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        ILogger<UserManager> logger = new LoggerDoubles.Fakes.FakeLogger<UserManager>();
        userRepository ??= new UserRepositoryDoubles.Dummy();
        passwordHasher ??= new PasswordHasherDoubles.Dummy();
        authenticationTokenProvider ??= new AuthenticationTokenProviderDoubles.Dummy();

        return new UserManager(logger, userRepository, passwordHasher, authenticationTokenProvider);
    }

    public sealed class GetAuthenticationTokenAsync : UserManagerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Should_ThrowArgumentException_WhenEmailIsInvalid(string? invalidEmail)
        {
            // Arrange
            string password = "Password123!";
            UserManager sut = GetSystemUnderTest();

            // Act & Assert
            ArgumentException argEx = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.GetAuthenticationTokenAsync(invalidEmail!, password));
            Assert.Equal("email", argEx.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task Should_ThrowArgumentException_WhenPasswordIsInvalid(string? invalidPassword)
        {
            // Arrange
            string email = "jukman@gmail.com";
            UserManager sut = GetSystemUnderTest();

            // Act & Assert
            ArgumentException argEx = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.GetAuthenticationTokenAsync(email, invalidPassword!));
            Assert.Equal("password", argEx.ParamName);
        }

        [Theory]
        [InlineData("password")]
        [InlineData("p@ssword")]
        [InlineData("p4ssw0rd")]
        [InlineData("Password")]
        [InlineData("PASSWORD")]
        [InlineData("P@SSWORD")]
        [InlineData("PASSWORD123")]
        [InlineData("P@55")]
        public async Task Should_ThrowArgumentException_WhenPasswordDoesNotMeetComplexityRequirements(string weakPassword)
        {
            // Arrange
            string email = "jukman@gmail.com";
            UserManager sut = GetSystemUnderTest();

            // Act & Assert
            ArgumentException argEx = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.GetAuthenticationTokenAsync(email, weakPassword));
            Assert.Equal("password", argEx.ParamName);
        }

        [Fact]
        public async Task Should_ThrowEntityNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "notjukman@gmail.com";
            string password = "Password123!";

            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.AlwaysReturnsNull());

            // Act & Assert
            EntityNotFoundException ex = await Assert.ThrowsAnyAsync<EntityNotFoundException>(async () => await sut.GetAuthenticationTokenAsync(email, password));
            Assert.Equal(nameof(User), ex.EntityType);
            Assert.Equal(nameof(User.Email), ex.KeyName);
            Assert.Equal(email, ex.Key);
        }

        [Fact]
        public async Task Should_GetUserFromRepository_WhenEmailIsValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();
            UserRepositoryDoubles.Spies.GetUserByEmail userRepositorySpy = new(
                spiedRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.ReturnsSpecificUser(user));
            UserManager sut = GetSystemUnderTest(
                userRepository: userRepositorySpy,
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid(),
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());

            // Act
            AuthenticationTokenResponse authenticationToken = await sut.GetAuthenticationTokenAsync(email, password);

            // Assert
            Assert.Equal(1, userRepositorySpy.InvokationCount);
            Assert.Equal(email, userRepositorySpy.LastReceivedEmail);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_WhenPasswordDoesNotMatch()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "WrongPassword123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword("Password123!")
                .Build();
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.ReturnsSpecificUser(user),
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysInvalid());

            // Act & Assert
            ArgumentException ex = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.GetAuthenticationTokenAsync(email, password));
            Assert.Equal("password", ex.ParamName);
        }

        [Fact]
        public async Task Should_VerifyPasswordInPasswordHasher_WhenPasswordIsValId()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();
            PasswordHasherDoubles.Spies.VerifySpy passwordHasherSpy = new(
                spiedHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.ReturnsSpecificUser(user),
                passwordHasher: passwordHasherSpy,
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());

            // Act
            await sut.GetAuthenticationTokenAsync(email, password);

            // Assert
            Assert.Equal(1, passwordHasherSpy.InvokationCount);
            Assert.Equal(Encoding.UTF8.GetBytes(password), passwordHasherSpy.LastReceivedPasswordBytes);
            Assert.Equal(Convert.FromBase64String(user.Salt), passwordHasherSpy.LastReceivedSalt);
            Assert.Equal(Convert.FromBase64String(user.PasswordHash), passwordHasherSpy.LastReceivedHash);
        }

        [Fact]
        public async Task Should_GetAuthenticationTokenFromAuthenticationTokenProvider_WhenCredentialsAreValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();

            AuthenticationTokenProviderDoubles.Spies.GenerateTokenSpy authTokenProviderSpy = new(
                new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.ReturnsSpecificUser(user),
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid(),
                authenticationTokenProvider: authTokenProviderSpy);

            // Act
            await sut.GetAuthenticationTokenAsync(email, password);

            // Assert
            Assert.Equal(1, authTokenProviderSpy.InvokationCount);
            Assert.Equal(user, authTokenProviderSpy.LastReceivedUser);
        }

        [Fact]
        public async Task Should_ReturnValidAuthenticationToken_WhenCredentialsAreValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();
            AuthenticationTokenProviderDoubles.Fakes.JsonSerializedTokenProvider jsonTokenProvider = new();
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetUserByEmail.ReturnsSpecificUser(user),
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid(),
                authenticationTokenProvider: jsonTokenProvider);

            // Act
            AuthenticationTokenResponse tokenResponse = await sut.GetAuthenticationTokenAsync(email, password);

            // Assert
            Assert.NotNull(tokenResponse);
            Assert.Equal(jsonTokenProvider.TokenType, tokenResponse.TokenType);
            Assert.True(jsonTokenProvider.IsValidToken(tokenResponse.Token));
            (IEnumerable<(string Key, string Value)> Claims, DateTimeOffset Expiration) = AuthenticationTokenProviderDoubles.Fakes.JsonSerializedTokenProvider.DecodeToken(tokenResponse.Token);
            string? emailClaim = Claims.FirstOrDefault(c => c.Key == "email").Value;
            Assert.Equal(email, emailClaim);
        }
    }
}
