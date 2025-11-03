using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class InMemoryUserRepository : IUserRepository
{
    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        return InMemoryData.Users.SingleOrDefault(u => u.Email == email);
    }

    public async Task<Guid> InsertAsync(User user, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        User userToAdd = User.Reconstitute(
            Guid.NewGuid(),
            user.Email,
            user.PasswordHash,
            user.Salt);
        InMemoryData.Users.Add(userToAdd);
        return user.Id;
    }
}
