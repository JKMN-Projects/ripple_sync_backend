using RippleSync.Application.Platforms;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Posts;

namespace RippleSync.Tests.Common.TestDoubles.Platforms;

public static class SoMePlatformDoubles
{
    public class Dummy : ISoMePlatform
    {
        public virtual string GetAuthorizationUrl(AuthorizationConfiguration authConfigs) => throw new NotImplementedException();
        public virtual Task<PlatformStats> GetInsightsFromIntegrationAsync(Integration integration, IEnumerable<Post> publishedPostsOnPlatform) => throw new NotImplementedException();
        public virtual HttpRequestMessage GetTokenRequest(TokenAccessConfiguration tokenConfigs) => throw new NotImplementedException();
        public virtual Task<PostEvent> PublishPostAsync(Post post, Integration integration) => throw new NotImplementedException();
    }

    public static class Stubs
    {
        public static class PublishPostAsync
        {
            public class ReturnsPostEventForIntegration : Dummy
            {
                public override Task<PostEvent> PublishPostAsync(Post post, Integration integration)
                    => Task.FromResult(post.PostEvents.Single(pe => pe.UserPlatformIntegrationId == integration.Id));
            }

            public class Throws : Dummy
            {
                public override Task<PostEvent> PublishPostAsync(Post post, Integration integration)
                    => throw new InvalidOperationException("Simulated exception in PublishPostAsync");
            }
        }
    }

    public static class Spies
    {
        public class PublishPostAsyncSpy : Dummy
        {
            private readonly ISoMePlatform _spied;
            private readonly List<Post> _posts = [];
            private readonly List<Integration> _integrations = [];

            public int InvocationCount { get; private set; }
            public IReadOnlyList<Post> Posts => _posts.AsReadOnly();
            public IReadOnlyList<Integration> Integrations => _integrations.AsReadOnly();
            public Post? LatestPost => _posts.LastOrDefault();
            public Integration? LatestIntegration => _integrations.LastOrDefault();

            public PublishPostAsyncSpy(ISoMePlatform spied)
            {
                _spied = spied;
            }

            public override Task<PostEvent> PublishPostAsync(Post post, Integration integration)
            {
                InvocationCount++;
                _posts.Add(post);
                _integrations.Add(integration);
                return _spied.PublishPostAsync(post, integration);
            }
        }
    }
}