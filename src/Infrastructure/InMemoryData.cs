using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Posts;
using RippleSync.Domain.Users;

namespace RippleSync.Infrastructure;

internal class InMemoryData
{
    private static readonly Guid _userId = Guid.Parse("a9856986-14e4-464b-acc7-dcb84ddf9f36");

    internal static readonly List<User> Users = [
            User.Reconstitute(_userId, "jukman@gmail.com", "hyT8uOvqa5HsVzoYa7f8x5Fc79whJ85hnUVlthmk2Ak=", "VGVzdGluZ0FTYWx0VmFsdWVXcml0dGVuSW5QbGFpblRleHQ=", DateTime.UtcNow, null)
        ];

    internal static readonly List<Integration> Integrations =
    [
        Integration.Reconstitute(
            Guid.NewGuid(),
            _userId,
            Platform.X,
            "",
            "",
            DateTime.UtcNow.AddHours(1),
            "Bearer",
            "read write"
        ),
        Integration.Reconstitute(
            Guid.NewGuid(),
            _userId,
            Platform.LinkedIn,
            "",
            "",
            DateTime.UtcNow.AddHours(2),
            "Bearer",
            "r_liteprofile w_member_social"
        )
    ];

    internal static readonly List<Post> Posts =
    [
        Post.Reconstitute(Guid.NewGuid(), _userId, "My first post", DateTime.UtcNow, null, DateTime.UtcNow.AddDays(2), [],
            [PostEvent.Reconstitute(Guid.NewGuid(), Guid.NewGuid(), PostStatus.Posted, "123456", null)]),

        Post.Reconstitute(Guid.NewGuid(), _userId, "My Scheduled post", DateTime.UtcNow, null, DateTime.UtcNow.AddDays(5), [],
            [PostEvent.Reconstitute(Guid.NewGuid(), Guid.NewGuid(), PostStatus.Scheduled, "654321", null)]),

        Post.Reconstitute(Guid.NewGuid(), _userId,"Stuck while processing", DateTime.UtcNow.AddDays(-2), null, DateTime.UtcNow.AddDays(-2), [],
            [PostEvent.Reconstitute(Guid.NewGuid(), Guid.NewGuid(), PostStatus.Processing, "", null)]),

        Post.Reconstitute(Guid.NewGuid(), _userId,"My post will not upload", DateTime.UtcNow.AddDays(-2), null, DateTime.UtcNow.AddDays(-2), [],
            [PostEvent.Reconstitute(Guid.NewGuid(), Guid.NewGuid(), PostStatus.Failed, "", "Error")]),

        Post.Reconstitute(Guid.NewGuid(), _userId,"Just created this post - NOT DONE", DateTime.UtcNow, null, null, [],
            [PostEvent.Reconstitute(Guid.NewGuid(), Guid.NewGuid(), PostStatus.Draft, "", null)]),
    ];
}
