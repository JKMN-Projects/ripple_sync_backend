
namespace RippleSync.Domain.Posts;

public class PostMedia
{
    public Guid Id { get; set; }
    public string ImageData { get; set; }

    private PostMedia(Guid id, string imageData)
    {
        Id = id;
        ImageData = imageData;
    }

    public static PostMedia New(string imageData)
    {
        return new PostMedia(
            id: Guid.NewGuid(),
            imageData: imageData
        );
    }

    public static PostMedia Reconstitute(Guid id, string imageData)
    {
        return new PostMedia(
            id: id,
            imageData: imageData
        );
    }

    public PostMedia Anonymize()
    {
        ImageData = string.Empty;
        return this;
    }
}
