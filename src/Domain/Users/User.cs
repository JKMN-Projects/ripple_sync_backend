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
        => new User(id: Guid.Empty, email: email, passwordHash: passwordHash, salt: salt, refreshToken: null);

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

    public void SetRefreshToken(RefreshToken refreshToken) => RefreshToken = refreshToken;

    public User Anonymize()
    {
        Email = Guid.NewGuid().ToString();
        PasswordHash = "";
        Salt = "";
        RefreshToken = null;
        return this;
    }
}
