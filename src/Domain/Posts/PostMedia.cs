
namespace RippleSync.Domain.Posts;

public class PostMedia
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public required string ImageUrl { get; set; }

    public PostMedia Anonymize()
    {
        ImageUrl = string.Empty;
        return this;
    }
}
