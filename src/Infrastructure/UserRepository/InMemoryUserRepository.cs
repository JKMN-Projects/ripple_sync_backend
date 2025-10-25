using RippleSync.Application.Common.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.UserRepository;
internal class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await Task.Delay(3000, cancellationToken);

        return _users.SingleOrDefault(u => u.Email == email);
    }
}
