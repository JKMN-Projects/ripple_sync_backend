using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Platforms;
using RippleSync.Application.Posts;
using RippleSync.Tests.Shared.TestDoubles.Logging;
using RippleSync.Tests.Shared.TestDoubles.Repositories;

namespace RippleSync.Application.Tests;

public abstract class PostManagerTests
{
    protected PostManager GetSystemUnderTest(
        ILogger<PostManager>? logger = null,
        IPostRepository? postRepository = null,
        IPostQueries postQueries = null,
        IIntegrationRepository? integrationRepository = null,
        IPlatformFactory? platformFactory = null)
    {
        logger ??= new LoggerDoubles.Fakes.FakeLogger<PostManager>();
        postRepository ??= new PostRepositoryDoubles.Dummy();
        //postQueries ??= new PostQueriesDoubles.Dummy();
        integrationRepository ??= new IntegrationRepositoryDoubles.Dummy();
        //platformFactory ??= new PlatformFactoryDoubles.Dummy();

        return new PostManager(
            logger,
            postRepository,
            postQueries,
            integrationRepository,
            platformFactory
        );
    }

    public sealed class ProcessPostEventAsync : PostManagerTests
    {

    }
}
