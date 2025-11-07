
namespace RippleSync.Domain.Posts;

public enum PostStatus
{
    Draft = 0,
    Scheduled,
    Processing,
    Posted,
    Failed
}