namespace RippleSync.Domain.Users;

public class User
{
    public Guid Id { get; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Salt { get; private set; }

    public RefreshToken? RefreshToken { get; private set; }

    private User(Guid id, string email, string passwordHash, string salt, RefreshToken? refreshToken)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Salt = salt;
        RefreshToken = refreshToken;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="User"/> class with the specified values.
    /// </summary>
    /// <param name="email">The email address associated with the user. Must not be null or empty.</param>
    /// <param name="passwordHash">The hashed password for the user. Must not be null or empty.</param>
    /// <param name="salt">The cryptographic salt used for hashing the password. Must not be null or empty.</param>
    /// <returns>A new <see cref="User"/> instance.</returns>
    public static User Create(string email, string passwordHash, string salt) 
        => new User(id: Guid.NewGuid(), email: email, passwordHash: passwordHash, salt: salt, refreshToken: null);

    /// <summary>
    /// Recreates an existing <see cref="User"/> instance with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="email">The email address associated with the user. Cannot be null or empty.</param>
    /// <param name="passwordHash">The hashed password of the user. Cannot be null or empty.</param>
    /// <param name="salt">The cryptographic salt used for hashing the password. Cannot be null or empty.</param>
    /// <returns>The existing <see cref="User"/> instance.</returns>
    public static User Reconstitute(Guid id, string email, string passwordHash, string salt, RefreshToken? refreshToken) 
        => new User(id: id, email: email, passwordHash: passwordHash, salt: salt, refreshToken: refreshToken);

    /// <summary>
    /// Adds a refresh token to the user.
    /// </summary>
    /// <param name="refreshToken"></param>
    public void AddRefreshToken(RefreshToken refreshToken) => RefreshToken = refreshToken;

    /// <summary>
    /// Verifies if the provided refresh token is valid and not expired. Revokes the token if invalid.
    /// </summary>
    /// <param name="token"></param>
    /// <param name="timeProvider"></param>
    /// <returns>The validity of the refresh token.</returns>
    public bool VerifyRefreshToken(string token, TimeProvider timeProvider)
    {
        bool isValid = RefreshToken is not null &&
               RefreshToken.Value == token &&
               !RefreshToken.IsExpired(timeProvider);

        if (!isValid)
        {
            RefreshToken = null;
        }

        return isValid;
    }

    public User Anonymize()
    {
        Email = Guid.NewGuid().ToString();
        PasswordHash = "";
        Salt = "";
        RefreshToken = null;
        return this;
    }
}
