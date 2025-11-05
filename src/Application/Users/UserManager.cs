
using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Users.Exceptions;
using RippleSync.Domain.Users;
using System.Text;

namespace RippleSync.Application.Users;

public sealed class UserManager(
    ILogger<UserManager> logger,
    IUserRepository userRepository,
    IIntegrationRepository integrationRepository,
    IPostRepository postRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationTokenProvider authenticationTokenProvider)
{

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
        return new AuthenticationTokenResponse(token.AccessToken, token.TokenType, token.ExpiresInMilliSeconds);
    }

    /// <summary>
    /// Registers a new user with the given email and password.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="password"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RegisterUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Registering new user with email {Email}", email);
        ArgumentException.ThrowIfNullOrWhiteSpace(email, nameof(email));
        if (!email.Contains('@') || !email.Contains('.')) throw new ArgumentException("Invalid email format", nameof(email));
        if (email.Split('@', StringSplitOptions.RemoveEmptyEntries).Length != 2) throw new ArgumentException("Invalid email format", nameof(email));
        if (email.Split('.', StringSplitOptions.RemoveEmptyEntries).Length < 2) throw new ArgumentException("Invalid email format", nameof(email));

        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
        if (password.Length < 8) throw new ArgumentException("Password must be at least 8 characters long.", nameof(password));
        else if (!password.Any(char.IsDigit)) throw new ArgumentException("Password must contain at least one digit.", nameof(password));
        else if (!password.Any(char.IsUpper)) throw new ArgumentException("Password must contain at least one uppercase letter.", nameof(password));
        else if (!password.Any(char.IsLower)) throw new ArgumentException("Password must contain at least one lowercase letter.", nameof(password));
        else if (!password.Any(ch => !char.IsLetterOrDigit(ch))) throw new ArgumentException("Password must contain at least one special character.", nameof(password));

        User? existingUser = await userRepository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null) throw new EmailAlreadyInUseException(email);

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] userSalt = passwordHasher.GenerateSalt();
        byte[] passwordHash = passwordHasher.Hash(passwordBytes, userSalt);
        User newUser = User.Create(
            email,
            Convert.ToBase64String(passwordHash),
            Convert.ToBase64String(userSalt)
        );

        await userRepository.InsertAsync(newUser, cancellationToken);
        logger.LogInformation("User with email {Email} registered successfully", email);
    }


    /// <summary>
    /// Soft deletes a user by anonymizing their data.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting user with ID {UserId}", userId);
        User? user = await userRepository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw EntityNotFoundException.ForEntity<User>(userId);
        }
        // Anonymize user data
        user.Anonymize();
        await userRepository.UpdateAsync(user, cancellationToken);

        var integrations = await integrationRepository.GetByUserIdAsync(userId, cancellationToken);
        foreach (var integration in integrations)
        {
            integration.Anonymize();
            await integrationRepository.UpdateAsync(integration, cancellationToken);
        }
        var posts = await postRepository.GetAllByUserIdAsync(userId, cancellationToken);
        foreach (var post in posts)
        {
            post.Anonymize();
            await postRepository.UpdatePostAsync(post, cancellationToken);
        }
        logger.LogInformation("User with ID {UserId} deleted successfully", userId);
    }
}

