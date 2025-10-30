using RippleSync.Domain.Users;

namespace RippleSync.Application.Common.Repositories;
public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Guid> InsertUserAsync(User user, CancellationToken cancellationToken = default);
}
