using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.Tests.Configuration;
using RippleSync.Infrastructure.UserRepository;
using RippleSync.Tests.Shared.Factories.Integrations;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Security;

namespace RippleSync.Infrastructure.Tests.Repositories;

public class NpgSqlIntegrationRepository : RepositoryTestBase
{
    private readonly NpgsqlIntegrationRepository _sut;

    public NpgSqlIntegrationRepository(PostgresDatabaseFixture fixture) : base(fixture)
    {
        _sut = new NpgsqlIntegrationRepository(UnitOfWork);
    }

    public override async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    public sealed class GetByUserIdAsync : NpgSqlIntegrationRepository
    {
        public GetByUserIdAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnEmptyList_WhenUserHasNoIntegrations()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await _sut.CreateAsync(integration1);
            await _sut.CreateAsync(integration2);
            Guid userId = Guid.NewGuid();

            // Act
            var integrations = await _sut.GetByUserIdAsync(userId);

            // Assert
            Assert.Empty(integrations);
        }

        [Fact]
        public async Task Should_ReturnIntegrations_WhenUserHasIntegrations()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await _sut.CreateAsync(integration1);
            await _sut.CreateAsync(integration2);

            // Act
            var integrations = await _sut.GetByUserIdAsync(user.Id);

            // Assert
            Assert.Equal(2, integrations.Count());
            Assert.Contains(integrations, i => i.Id == integration1.Id);
            Assert.Contains(integrations, i => i.Id == integration2.Id);
        }
    }

    public sealed class CreateAsync : NpgSqlIntegrationRepository
    {
        public CreateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_SaveIntegrationToDatabase()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();

            // Act
            await _sut.CreateAsync(integration);

            // Assert
            var retrievedIntegrations = await _sut.GetByUserIdAsync(user.Id);
            Assert.Single(retrievedIntegrations);
            var retrievedIntegration = retrievedIntegrations.First();
            Assert.Equal(integration.Id, retrievedIntegration.Id);
            Assert.Equal(integration.AccessToken, retrievedIntegration.AccessToken);
            Assert.Equal(integration.RefreshToken, retrievedIntegration.RefreshToken);
            Assert.Equal(integration.TokenType, retrievedIntegration.TokenType);
            Assert.Equal(integration.Scope, retrievedIntegration.Scope);
        }
    }

    public sealed class UpdateAsync : NpgSqlIntegrationRepository
    {
        public UpdateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_UpdateIntegrationInDatabase()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await _sut.CreateAsync(integration);
            integration.Anonymize();

            // Act
            await _sut.UpdateAsync(integration);
            
            // Assert
            var retrievedIntegrations = await _sut.GetByUserIdAsync(user.Id);
            Assert.Single(retrievedIntegrations);
            var retrievedIntegration = retrievedIntegrations.First();
            Assert.Equal(integration.Id, retrievedIntegration.Id);
            Assert.Empty(retrievedIntegration.AccessToken);
            Assert.Empty(retrievedIntegration.RefreshToken);
        }

        [Fact]
        public async Task Should_ThrowRepositoryException_WhenUpdatingNonExistentIntegration()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();

            // Act & Assert
            await Assert.ThrowsAsync<RepositoryException>(() =>  _sut.UpdateAsync(integration));
        }
    }

    public sealed class DeleteAsync : NpgSqlIntegrationRepository
    {
        public DeleteAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_DeleteIntegrationFromDatabase()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await _sut.CreateAsync(integration);

            // Act
            await _sut.DeleteAsync(user.Id, integration.Platform);

            // Assert
            var retrievedIntegrations = await _sut.GetByUserIdAsync(user.Id);
            Assert.Empty(retrievedIntegrations);
        }

        [Fact]
        public async Task Should_ThrowRepositoryException_WhenDeletingNonExistentIntegration()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<RepositoryException>(() => _sut.DeleteAsync(user.Id, Platform.X));
        }
    }

    public sealed class GetIntegrationsByIdsAsync : NpgSqlIntegrationRepository
    {
        public GetIntegrationsByIdsAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnIntegrations_WhenIntegrationsExist()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await _sut.CreateAsync(integration1);
            await _sut.CreateAsync(integration2);
            var integrationIds = new List<Guid> { integration1.Id, integration2.Id };

            // Act
            var integrations = await _sut.GetIntegrationsByIdsAsync(integrationIds);

            // Assert
            Assert.Equal(2, integrations.Count());
            Assert.Contains(integrations, i => i.Id == integration1.Id);
            Assert.Contains(integrations, i => i.Id == integration2.Id);
        }
    }

    public sealed class GetConnectedIntegrationsAsync : NpgSqlIntegrationRepository
    {
        public GetConnectedIntegrationsAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnConnectedIntegrations_WhenUserHasIntegrations()
        {
            // Arrange
            IUserRepository userRepository = new NpgsqlUserRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            var integration1 = Integration.Create(
                user.Id,
                Platform.X,
                "accessToken1",
                "refreshToken1",
                DateTime.UtcNow.AddHours(1),
                "Bearer",
                "scope1"
            );
            var integration2 = Integration.Create(
                user.Id,
                Platform.Facebook,
                "accessToken2",
                "refreshToken2",
                DateTime.UtcNow.AddHours(1),
                "Bearer",
                "scope2"
            );
            await _sut.CreateAsync(integration1);
            await _sut.CreateAsync(integration2);
            // Act
            var connectedIntegrations = await _sut.GetConnectedIntegrationsAsync(user.Id);
            // Assert
            Assert.Equal(2, connectedIntegrations.Count());
            Assert.Contains(connectedIntegrations, i => i.UserPlatformIntegrationId == integration1.Id && i.PlatFormName == Platform.X.ToString());
            Assert.Contains(connectedIntegrations, i => i.UserPlatformIntegrationId == integration2.Id && i.PlatFormName == Platform.Facebook.ToString());
        }
    }
}
