using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.PlatformRepository;
using RippleSync.Infrastructure.Tests.Configuration;
using RippleSync.Infrastructure.UserRepository;
using RippleSync.Tests.Shared.Factories.Integrations;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Security;

namespace RippleSync.Infrastructure.Tests.Repositories;
public class NpgsqlPlatformRepositoryTests : RepositoryTestBase
{
    private readonly NpgsqlPlatformRepository _sut;
    public NpgsqlPlatformRepositoryTests(PostgresDatabaseFixture fixture) : base(fixture)
    {
        _sut = new NpgsqlPlatformRepository(UnitOfWork);
    }

    public override async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    public sealed class GetPlatformsWithUserIntegrationsAsync : NpgsqlPlatformRepositoryTests
    {
        public GetPlatformsWithUserIntegrationsAsync(PostgresDatabaseFixture fixture) : base(fixture) { }

        [Fact]
        public async Task Should_ReturnAllPlatformsAsNotConnected_WhenNoUserIntegrations()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var platforms = await _sut.GetPlatformsWithUserIntegrationsAsync(userId);

            // Assert
            Assert.NotNull(platforms);
            Assert.Equal(Enum.GetValues<Platform>().Length, platforms.Count());
            foreach (var platform in platforms)
            {
                Assert.False(platform.Connected);
            }
        }

        [Fact]
        public async Task Should_ReturnPlatformsWithCorrectConnectionStatus_WhenUserHasIntegrations()
        {
            // Arrange
            var userRepository = new NpgsqlUserRepository(UnitOfWork);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await integrationRepository.CreateAsync(integration1);
            await integrationRepository.CreateAsync(integration2);

            // Act
            var platforms = await _sut.GetPlatformsWithUserIntegrationsAsync(user.Id);

            // Assert
            Assert.NotNull(platforms);
            Assert.Equal(Enum.GetValues<Platform>().Length, platforms.Count());
            foreach (var platform in platforms)
            {
                if (platform.PlatformId == (int)integration1.Platform || platform.PlatformId == (int)integration2.Platform)
                {
                    Assert.True(platform.Connected);
                }
                else
                {
                    Assert.False(platform.Connected);
                }
            }
        }
    }
}
