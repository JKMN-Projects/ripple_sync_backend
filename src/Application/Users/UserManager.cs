
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Domain.Users;
using System.Text;

namespace RippleSync.Application.Users;

public sealed class UserManager
{
    private readonly ILogger<UserManager> logger;
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly IAuthenticationTokenProvider authenticationTokenProvider;

    public UserManager(ILogger<UserManager> logger, IUserRepository userRepository, IPasswordHasher passwordHasher, IAuthenticationTokenProvider authenticationTokenProvider)
    {
        this.logger = logger;
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.authenticationTokenProvider = authenticationTokenProvider;
    }

    /// <summary>
    /// Generates an authentication token for a user with the given email and password.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="EntityNotFoundException"></exception>
    public async Task<AuthenticationTokenResponse> GetAuthenticationTokenAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));

        if (password.Length < 8) throw new ArgumentException("Password must be at least 8 characters long.", nameof(password));
        else if (!password.Any(char.IsDigit)) throw new ArgumentException("Password must contain at least one digit.", nameof(password));
        else if (!password.Any(char.IsUpper)) throw new ArgumentException("Password must contain at least one uppercase letter.", nameof(password));
        else if (!password.Any(char.IsLower)) throw new ArgumentException("Password must contain at least one lowercase letter.", nameof(password));
        else if (!password.Any(ch => !char.IsLetterOrDigit(ch))) throw new ArgumentException("Password must contain at least one special character.", nameof(password));

        User? user = await userRepository.GetUserByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            throw EntityNotFoundException.ForEntity<User>(email, nameof(User.Email));
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] salt = Convert.FromBase64String(user.Salt);
        byte[] passwordHash = Convert.FromBase64String(user.PasswordHash);

        if (!passwordHasher.Verify(passwordBytes, salt, passwordHash))
        {
            throw new ArgumentException("Invalid password", nameof(password));
        }

        logger.LogInformation("Generating authentication token for user with email {Email}", email);
        AuthenticationToken token = await authenticationTokenProvider.GenerateTokenAsync(user, cancellationToken);
        return new AuthenticationTokenResponse(token.AccessToken, token.TokenType, token.ExpiresInSeconds);
    }
}
