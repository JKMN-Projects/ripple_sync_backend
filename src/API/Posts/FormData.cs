namespace RippleSync.API.Posts;

public partial class PostsController
{
    public class FormData
    {
        public Guid? PostId { get; set; }

        public required string MessageContent { get; set; }

        public long? Timestamp { get; set; }

        public List<Guid>? IntegrationIds { get; set; }

        public List<IFormFile>? Files { get; set; }
    }
}

