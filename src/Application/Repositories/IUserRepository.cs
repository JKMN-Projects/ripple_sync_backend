using RippleSync.Domain.Users;

namespace RippleSync.Application.Repositories;
public interface IUserRepository
{
    public Task<User?> GetUserByEmail(string email);
}
