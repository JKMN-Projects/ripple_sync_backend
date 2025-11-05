using RippleSync.Domain.Users;

namespace RippleSync.Application.Common.Repositories;

public interface IUserRepository
{
    Task InsertAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}