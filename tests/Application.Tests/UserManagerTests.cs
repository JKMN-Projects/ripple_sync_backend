using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Users;
using RippleSync.Application.Users.Exceptions;
using RippleSync.Domain.Users;
using RippleSync.Tests.Common.Factories.Users;
using RippleSync.Tests.Common.TestDoubles;
using RippleSync.Tests.Common.TestDoubles.Logging;
using RippleSync.Tests.Common.TestDoubles.Repositories;
using RippleSync.Tests.Common.TestDoubles.Security;
using System.Text;

namespace RippleSync.Application.Tests;

public abstract class UserManagerTests
{
    protected static UserManager GetSystemUnderTest(
        IUserRepository? userRepository = null,
        TimeProvider? timeProvider = null,
        IPostRepository? postRepository = null,
        IIntegrationRepository? integrationRepository = null,
        IPasswordHasher? passwordHasher = null,
        IAuthenticationTokenProvider? authenticationTokenProvider = null)
    {
        ILogger<UserManager> logger = new LoggerDoubles.Fakes.FakeLogger<UserManager>();
        timeProvider ??= TimeProvider.System;
        userRepository ??= new UserRepositoryDoubles.Dummy();
        integrationRepository ??= new IntegrationRepositoryDoubles.Dummy();
        postRepository ??= new PostRepositoryDoubles.Dummy();
        passwordHasher ??= new PasswordHasherDoubles.Dummy();
        authenticationTokenProvider ??= new AuthenticationTokenProviderDoubles.Dummy();
        IUnitOfWork ouw = new UnitOfWorkDoubles.Fakes.DoesNothing();

        return new UserManager(logger, timeProvider, ouw, userRepository, integrationRepository, postRepository, passwordHasher, authenticationTokenProvider);
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

        [Fact]
        public async Task Should_ThrowEntityNotFoundException_WhenUserDoesNotExist()
        {
            // Arrange
            string email = "notjukman@gmail.com";
            string password = "Password123!";

            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetByEmail.AlwaysReturnsNull());

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
            UserRepositoryDoubles.Spies.GetByEmail userRepositorySpy = new(
                spiedRepository: new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user));
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Composite(
                    userRepositorySpy,
                    new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser()
                ),
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
                userRepository: new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user),
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
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user),
                    new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser()
                ),
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
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user),
                    new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser()),
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid(),
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Composite(
                    authTokenProviderSpy,
                    new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider())
                );

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
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user),
                    new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser()
                ),
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

        [Fact]
        public async Task Should_AddRefreshTokenTo_WhenCredentialsAreValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();
            var updateUserSpy = new UserRepositoryDoubles.Spies.Update(
                spiedRepository: new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(user),
                    updateUserSpy
                ),
                passwordHasher: new PasswordHasherDoubles.Stubs.Verify.AlwaysValid(),
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());

            // Act
            await sut.GetAuthenticationTokenAsync(email, password);

            // Assert
            Assert.NotNull(user.RefreshToken);
            Assert.Equal(1, updateUserSpy.InvokationCount);
            Assert.Equal(user.Email, updateUserSpy.LastReceivedUser?.Email);
            Assert.NotNull(updateUserSpy.LastReceivedUser?.RefreshToken);
        }
    }

    public sealed class RegisterUserAsync : UserManagerTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("notAnEmail")]
        [InlineData("notAnEmail@")]
        [InlineData("@email.com")]
        [InlineData("notAnEmail@email.")]
        public async Task Should_ThrowArgumentException_WhenEmailIsInvalid(string? email)
        {
            // Arrange
            string password = "Password123!";
            UserManager sut = GetSystemUnderTest();

            // Act & Assert
            ArgumentException argEx = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.RegisterUserAsync(email!, password));
            Assert.Equal("email", argEx.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("password")]
        [InlineData("p@ssword")]
        [InlineData("p4ssw0rd")]
        [InlineData("Password")]
        [InlineData("PASSWORD")]
        [InlineData("P@SSWORD")]
        [InlineData("PASSWORD123")]
        [InlineData("P@55")]
        public async Task Should_ThrowArgumentException_WhenPasswordIsInvalid(string? invalidPassword)
        {
            // Arrange
            string email = "jukman@gmail.com";
            UserManager sut = GetSystemUnderTest();

            // Act & Assert
            ArgumentException argEx = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.RegisterUserAsync(email, invalidPassword!));
            Assert.Equal("password", argEx.ParamName);
        }

        [Fact]
        public async Task Should_ThrowEmailAlreadyInUseException_WhenUserWithEmailAlreadyExists()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            User existingUser = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail(email)
                .WithPassword(password)
                .Build();
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetByEmail.ReturnsSpecificUser(existingUser));

            // Act & Assert
            EmailAlreadyInUseException ex = await Assert.ThrowsAnyAsync<EmailAlreadyInUseException>(async () => await sut.RegisterUserAsync(email, password));
            Assert.Equal(email, ex.Data["Email"]);
        }

        [Fact]
        public async Task Should_CheckIfUserExistsInRepository_WhenEmailIsValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            UserRepositoryDoubles.Spies.GetByEmail userRepositorySpy = new(
                spiedRepository: new UserRepositoryDoubles.Stubs.GetByEmail.AlwaysReturnsNull());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Composite(userRepositorySpy, new UserRepositoryDoubles.Stubs.Insert.AlwaysReturnsNewGuid()),
                passwordHasher: new PasswordHasherDoubles.Fakes.Base64Hasher());

            // Act
            await sut.RegisterUserAsync(email, password);

            // Assert
            Assert.Equal(1, userRepositorySpy.InvokationCount);
            Assert.Equal(email, userRepositorySpy.LastReceivedEmail);
        }

        [Fact]
        public async Task Should_CreateSaveUserInRepository_WhenEmailAndPasswordAreValid()
        {
            // Arrange
            string email = "jukman@gmail.com";
            string password = "Password123!";
            UserRepositoryDoubles.Spies.Insert insertUserSpy = new(
                spiedRepository: new UserRepositoryDoubles.Stubs.Insert.AlwaysReturnsNewGuid());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Composite(insertUserSpy, new UserRepositoryDoubles.Stubs.GetByEmail.AlwaysReturnsNull()),
                passwordHasher: new PasswordHasherDoubles.Fakes.Base64Hasher());

            // Act
            await sut.RegisterUserAsync(email, password);

            // Assert
            Assert.Equal(1, insertUserSpy.InvokationCount);
            User savedUser = insertUserSpy.LastReceivedUser!;
            Assert.Equal(email, savedUser.Email);
            Assert.NotEqual(password, savedUser.PasswordHash);
        }
    }

    public sealed class RefreshAuthenticationTokenAsync : UserManagerTests
    {
        [Fact]
        public async Task Should_ReturnNewAuthenticationTokenResponse_WhenRefreshTokenIsValid()
        {
            // Arrange
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail("jukman@gmail.com")
                .WithPassword("Password123!")
                .Build();
            AuthenticationTokenProviderDoubles.Fakes.JsonSerializedTokenProvider jsonTokenProvider = new();
            RefreshToken token = await jsonTokenProvider.GenerateRefreshTokenAsync(user);
            user.AddRefreshToken(token);
            var updateUserSpy = new UserRepositoryDoubles.Spies.Update(
                spiedRepository: new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser());
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByRefreshToken.ReturnsSpecificUser(user),
                    updateUserSpy),
                authenticationTokenProvider: jsonTokenProvider);

            // Act
            AuthenticationTokenResponse tokenResponse = await sut.RefreshAuthenticationTokenAsync(token.Value);

            // Assert
            Assert.NotNull(tokenResponse);
            Assert.Equal(jsonTokenProvider.TokenType, tokenResponse.TokenType);
            Assert.True(jsonTokenProvider.IsValidToken(tokenResponse.Token));
            Assert.NotEqual(token.Value, tokenResponse.RefreshToken);
            Assert.Equal(1, updateUserSpy.InvokationCount);
            Assert.Equal(user.Email, updateUserSpy.LastReceivedUser?.Email);
            Assert.Equal(tokenResponse.RefreshToken, updateUserSpy.LastReceivedUser?.RefreshToken?.Value);
        }

        [Fact]
        public async Task Should_ThrowEntityNotFoundException_WhenRefreshTokenIsInvalid()
        {
            // Arrange
            string invalidRefreshToken = "Invalid refresh token";
            UserManager sut = GetSystemUnderTest(
                userRepository: new UserRepositoryDoubles.Stubs.GetByRefreshToken.AlwaysReturnsNull());

            // Act & Assert
            EntityNotFoundException ex = await Assert.ThrowsAnyAsync<EntityNotFoundException>(async () => await sut.RefreshAuthenticationTokenAsync(invalidRefreshToken));
            Assert.Equal(nameof(User), ex.EntityType);
            Assert.Equal("RefreshToken", ex.KeyName);
            Assert.Equal(invalidRefreshToken, ex.Key);
        }

        [Fact]
        public async Task Should_ThrowArgumentException_WhenRefreshTokenIsExpired()
        {
            // Arrange
            FakeTimeProvider fakeTimeProvider = new(DateTime.UtcNow);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail("jukman@gmail.com")
                .WithPassword("Password123!")
                .WithRefreshToken(
                    RefreshToken.Create(
                        token: "ExpiredToken",
                        timeProvider: fakeTimeProvider,
                        expiresAt: fakeTimeProvider.GetUtcNow().AddDays(5).UtcDateTime))
                .Build();
            fakeTimeProvider.Advance(TimeSpan.FromDays(7));
            UserManager sut = GetSystemUnderTest(
                timeProvider: fakeTimeProvider,
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByRefreshToken.ReturnsSpecificUser(user),
                    new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser()),
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());

            // Act & Assert½
            ArgumentException ex = await Assert.ThrowsAnyAsync<ArgumentException>(async () => await sut.RefreshAuthenticationTokenAsync(user.RefreshToken!.Value));
            Assert.Equal("refreshToken", ex.ParamName);
        }

        [Fact]
        public async Task Should_RevokeRefreshToken_WhenRefreshTokenIsExpired()
        {
            // Arrange
            FakeTimeProvider fakeTimeProvider = new(DateTime.UtcNow);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail("jukman@gmail.com")
                .WithPassword("Password123!")
                .WithRefreshToken(
                    RefreshToken.Create(
                        token: "ExpiredToken",
                        timeProvider: fakeTimeProvider,
                        expiresAt: fakeTimeProvider.GetUtcNow().AddDays(5).UtcDateTime))
                .Build();
            fakeTimeProvider.Advance(TimeSpan.FromDays(7));
            var updateUserSpy = new UserRepositoryDoubles.Spies.Update(
                spiedRepository: new UserRepositoryDoubles.Stubs.Update.ReturnsReceivedUser());
            UserManager sut = GetSystemUnderTest(
                timeProvider: fakeTimeProvider,
                userRepository: new UserRepositoryDoubles.Composite(
                    new UserRepositoryDoubles.Stubs.GetByRefreshToken.ReturnsSpecificUser(user),
                    updateUserSpy),
                authenticationTokenProvider: new AuthenticationTokenProviderDoubles.Fakes.SimpleTokenProvider());

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await sut.RefreshAuthenticationTokenAsync(user.RefreshToken!.Value));
            Assert.Equal(1, updateUserSpy.InvokationCount);
            Assert.Null(updateUserSpy.LastReceivedUser?.RefreshToken);
        }
    }
}
