using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.UserRepository;
internal sealed class InMemoryUserRepository : IUserRepository
{
    private static readonly List<User> _users = [
            User.Reconstitute(Guid.Parse("a9856986-14e4-464b-acc7-dcb84ddf9f36"), "jukman@gmail.com", "hyT8uOvqa5HsVzoYa7f8x5Fc79whJ85hnUVlthmk2Ak=", "VGVzdGluZ0FTYWx0VmFsdWVXcml0dGVuSW5QbGFpblRleHQ=")
        ];

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        return _users.SingleOrDefault(u => u.Email == email);
    }

    public async Task<Guid> InsertUserAsync(User user, CancellationToken cancellationToken = default)
    {
        int delay = Random.Shared.Next(50, 400);
        await Task.Delay(delay, cancellationToken);

        User userToAdd = User.Reconstitute(
            Guid.NewGuid(),
            user.Email,
            user.PasswordHash,
            user.Salt);
        _users.Add(userToAdd);
        return user.Id;
    }
}
