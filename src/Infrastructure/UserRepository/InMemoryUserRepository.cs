using RippleSync.Application.Repositories;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure.UserRepository;
internal class InMemoryUserRepository : IUserRepository
{
    private List<User> _users = new List<User>()
    {
        new(Guid.NewGuid(), "jukman.test1@gmail.com")
    };

    public async Task<User?> GetUserByEmail(string email)
    {
        Thread.Sleep(3000);

        return _users.SingleOrDefault(u => u.Email == email);
    }
}
