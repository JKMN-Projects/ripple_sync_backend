
using RippleSync.Domain.Users;

namespace RippleSync.Application.Common.Security;
public interface IAuthenticationTokenProvider
{
    Task<AuthenticationToken> GenerateTokenAsync(User user, CancellationToken cancellationToken = default);
}
