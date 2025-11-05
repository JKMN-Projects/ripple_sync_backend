using Microsoft.Data.SqlClient;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.Tests.Configuration;
using RippleSync.Infrastructure.UserRepository;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Security;
using System.Text;

namespace RippleSync.Infrastructure.Tests.Repositories;

public class NpgsqlUserRepositoryTests : RepositoryTestBase
{
    private readonly NpgsqlUserRepository _sut;

    public NpgsqlUserRepositoryTests(PostgresDatabaseFixture fixture) : base(fixture)
    {
        _sut = new NpgsqlUserRepository(DbConnection);
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
            await DataSeeder.SeedUserAsync(user);
            string email = "notjukman@gmail.com";

            // Act
            User? receivedUser = await _sut.GetByEmailAsync(email);

            // Assert
            Assert.Null(receivedUser);
        }
    }
}
