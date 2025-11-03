
namespace RippleSync.Domain.Posts;

public enum PostStatus
{
    Draft = 0,
    Scheduled,
    Posted,
    Processing,
    Failed
}