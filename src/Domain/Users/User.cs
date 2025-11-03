
using System.Reflection.Metadata.Ecma335;

namespace RippleSync.Domain.Users;

public class User
{
    public Guid Id { get; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Salt { get; private set; }

    private User(Guid id, string email, string passwordHash, string salt)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        Salt = salt;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="User"/> class with the specified values.
    /// </summary>
    /// <param name="email">The email address associated with the user. Must not be null or empty.</param>
    /// <param name="passwordHash">The hashed password for the user. Must not be null or empty.</param>
    /// <param name="salt">The cryptographic salt used for hashing the password. Must not be null or empty.</param>
    /// <returns>A new <see cref="User"/> instance.</returns>
    public static User Create(string email, string passwordHash, string salt)
    {
        return new User(default, email, passwordHash, salt);
    }

    /// <summary>
    /// Recreates an existing <see cref="User"/> instance with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="email">The email address associated with the user. Cannot be null or empty.</param>
    /// <param name="passwordHash">The hashed password of the user. Cannot be null or empty.</param>
    /// <param name="salt">The cryptographic salt used for hashing the password. Cannot be null or empty.</param>
    /// <returns>The existing <see cref="User"/> instance.</returns>
    public static User Reconstitute(Guid id, string email, string passwordHash, string salt)
    {
        return new User(id, email, passwordHash, salt);
    }

    public User Anonymize()
    {
        Email = Guid.NewGuid().ToString();
        PasswordHash = "";
        Salt = "";
        return this;

    }
}
