using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class InMemoryUserRepository : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        return InMemoryData.Users.SingleOrDefault(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        return InMemoryData.Users.SingleOrDefault(u => u.Id == userId);
    }

    public Task<User?> GetByRefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        User? user = InMemoryData.Users.SingleOrDefault(u =>
            u.RefreshToken is not null && u.RefreshToken.Value == refreshTokenValue);
        return Task.FromResult(user);
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        User userToAdd = User.Reconstitute(
            Guid.NewGuid(),
            user.Email,
            user.PasswordHash,
            user.Salt,
            DateTime.UtcNow, null);
        InMemoryData.Users.Add(userToAdd);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);
    }
}
