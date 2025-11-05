
using RippleSync.Domain.Users;

namespace RippleSync.Application.Common.Security;
public interface IAuthenticationTokenProvider
{
    Task<RefreshToken> GenerateRefreshTokenAsync(User user, CancellationToken cancellationToken = default);
    Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);
}
