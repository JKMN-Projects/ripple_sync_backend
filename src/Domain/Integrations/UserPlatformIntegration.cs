namespace RippleSync.Domain.Integrations;
public class UserPlatformIntegration
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Connected { get; set; }
    public string ImageUrl { get; set; }

    private UserPlatformIntegration(Guid id, string name, string description, bool connected, string imageUrl)
    {
        Id = id;
        Name = name;
        Description = description;
        Connected = connected;
        ImageUrl = imageUrl;
    }

    /// <summary>
    /// Recreates an existing <see cref="UserPlatformIntegration"/> instance with the specified properties.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="email">The email address associated with the user. Cannot be null or empty.</param>
    /// <param name="passwordHash">The hashed password of the user. Cannot be null or empty.</param>
    /// <param name="salt">The cryptographic salt used for hashing the password. Cannot be null or empty.</param>
    /// <returns>The existing <see cref="UserPlatformIntegration"/> instance.</returns>
    public static UserPlatformIntegration Reconstitute(Guid id, string name, string description, bool connected, string imageUrl)
        => new(id, name, description, connected, imageUrl);
}

