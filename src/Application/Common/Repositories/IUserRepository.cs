using RippleSync.Domain.Users;

namespace RippleSync.Application.Common.Repositories;
public interface IUserRepository
{
    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
}
