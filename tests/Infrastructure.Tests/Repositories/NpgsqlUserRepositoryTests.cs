using RippleSync.Domain.Users;
using RippleSync.Infrastructure.Tests.Configuration;
using RippleSync.Infrastructure.UserRepository;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Security;

namespace RippleSync.Infrastructure.Tests.Repositories;

public class NpgsqlUserRepositoryTests : RepositoryTestBase
{
    private readonly NpgsqlUserRepository _sut;

    public NpgsqlUserRepositoryTests(PostgresDatabaseFixture fixture) : base(fixture)
    {
        _sut = new NpgsqlUserRepository(UnitOfWork);
    }

    public override async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    public sealed class GetByEmailAsync : NpgsqlUserRepositoryTests
    {
        public GetByEmailAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail("jukman@gmail.com")
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);
            string email = "notjukman@gmail.com";

            // Act
            User? receivedUser = await _sut.GetByEmailAsync(email);

            // Assert
            Assert.Null(receivedUser);
        }

        [Fact]
        public async Task Should_ReturnUserWithRefreshToken_WhenUserExists()
        {
            // Arrange
            var refreshToken = RefreshToken.Create(Guid.NewGuid().ToString(), TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithEmail("jukman@gmail.com")
                .WithRefreshToken(refreshToken)
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);

            // Act
            User? receivedUser = await _sut.GetByEmailAsync(user.Email);

            // Assert
            Assert.Equal(user.Id, receivedUser?.Id);
            Assert.Equal(user.Email, receivedUser?.Email);
            Assert.Equal(user.PasswordHash, receivedUser?.PasswordHash);
            Assert.Equal(user.Salt, receivedUser?.Salt);
            Assert.NotNull(receivedUser?.RefreshToken);
        }
    }

    public sealed class GetByIdAsync : NpgsqlUserRepositoryTests
    {
        public GetByIdAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);

            // Act
            User? receivedUser = await _sut.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(receivedUser);
        }

        [Fact]
        public async Task Should_ReturnUserWithRefreshToken_WhenUserExists()
        {
            // Arrange
            var refreshToken = RefreshToken.Create(Guid.NewGuid().ToString(), TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(refreshToken)
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);

            // Act
            User? receivedUser = await _sut.GetByIdAsync(user.Id);

            // Assert
            Assert.Equal(user.Id, receivedUser?.Id);
            Assert.Equal(user.Email, receivedUser?.Email);
            Assert.Equal(user.PasswordHash, receivedUser?.PasswordHash);
            Assert.Equal(user.Salt, receivedUser?.Salt);
            Assert.NotNull(receivedUser?.RefreshToken);
        }
    }

    public sealed class GetByRefreshTokenAsync : NpgsqlUserRepositoryTests
    {
        public GetByRefreshTokenAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnNull_WhenUserDoesNotExist()
        {
            // Arrange
            var refreshToken = RefreshToken.Create(Guid.NewGuid().ToString(), TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(refreshToken)
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);

            // Act
            User? receivedUser = await _sut.GetByRefreshTokenAsync(Guid.NewGuid().ToString());

            // Assert
            Assert.Null(receivedUser);
        }

        [Fact]
        public async Task Should_ReturnUserWithRefreshToken_WhenUserExists()
        {
            // Arrange
            var refreshToken = RefreshToken.Create(Guid.NewGuid().ToString(), TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(refreshToken)
                .Build();
            await _sut.CreateAsync(user);
            //await DataSeeder.SeedUserAsync(user);

            // Act
            User? receivedUser = await _sut.GetByIdAsync(user.Id);

            // Assert
            Assert.Equal(user.Id, receivedUser?.Id);
            Assert.Equal(user.Email, receivedUser?.Email);
            Assert.Equal(user.PasswordHash, receivedUser?.PasswordHash);
            Assert.Equal(user.Salt, receivedUser?.Salt);
            Assert.NotNull(receivedUser?.RefreshToken);
        }
    }

    public sealed class CreateAsync : NpgsqlUserRepositoryTests
    {
        public CreateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_SaveUserAndRefreshTokenToDatabase_WhenCreatingUserWithRefreshToken()
        {
            // Arrange
            var refreshToken = RefreshToken.Create(Guid.NewGuid().ToString(), TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(refreshToken)
                .Build();

            // Act
            await _sut.CreateAsync(user);

            // Assert
            User? receivedUser = await _sut.GetByIdAsync(user.Id);
            Assert.Equal(user.Id, receivedUser?.Id);
            Assert.Equal(user.Email, receivedUser?.Email);
            Assert.Equal(user.PasswordHash, receivedUser?.PasswordHash);
            Assert.Equal(user.Salt, receivedUser?.Salt);
            Assert.NotNull(receivedUser?.RefreshToken);
        }

        [Fact]
        public async Task Should_OnlySaveUserToDatabase_WhenCreatingUserWithoutRefreshToken()
        {
            // Arrange
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();

            // Act
            await _sut.CreateAsync(user);

            // Assert
            User? receivedUser = await _sut.GetByIdAsync(user.Id);
            Assert.Equal(user.Id, receivedUser?.Id);
            Assert.Equal(user.Email, receivedUser?.Email);
            Assert.Equal(user.PasswordHash, receivedUser?.PasswordHash);
            Assert.Equal(user.Salt, receivedUser?.Salt);
            Assert.Null(receivedUser?.RefreshToken);
        }
    }

    public sealed class UpdateAsync : NpgsqlUserRepositoryTests
    {
        public UpdateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_UpdateUserInDatabase()
        {
            // Arrange
            RefreshToken firstRefreshToken = RefreshToken.Create("first_token", TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(firstRefreshToken)
                .Build();
            await _sut.CreateAsync(user);
            RefreshToken secondRefreshToken = RefreshToken.Create("second_token", TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            user.AddRefreshToken(secondRefreshToken);

            // Act
            await _sut.UpdateAsync(user);

            // Assert
            User? persistetUser = await _sut.GetByIdAsync(user.Id);
            Assert.NotNull(persistetUser);
            Assert.NotEqual(secondRefreshToken.Value, firstRefreshToken.Value);
        }

        [Fact]
        public async Task Should_RemoveRefreshTokenFromDatabase_WhenUserRefreshTokenIsNull()
        {
            // Arrange
            RefreshToken firstRefreshToken = RefreshToken.Create("first_token", TimeProvider.System, TimeProvider.System.GetUtcNow().AddHours(1).UtcDateTime);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .WithRefreshToken(firstRefreshToken)
                .Build();
            await _sut.CreateAsync(user);
            user.RevokeRefreshToken();

            // Act
            await _sut.UpdateAsync(user);

            // Assert
            User? persistetUser = await _sut.GetByIdAsync(user.Id);
            Assert.NotNull(persistetUser);
            Assert.Null(persistetUser?.RefreshToken);
        }
    }
}
