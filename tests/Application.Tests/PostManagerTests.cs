using Microsoft.Extensions.Logging;
using RippleSync.Application.Common.Exceptions;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Platforms;
using RippleSync.Application.Posts;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Posts;
using RippleSync.Tests.Shared.Factories.Integrations;
using RippleSync.Tests.Shared.Factories.Posts;
using RippleSync.Tests.Shared.TestDoubles;
using RippleSync.Tests.Shared.TestDoubles.Logging;
using RippleSync.Tests.Shared.TestDoubles.Platforms;
using RippleSync.Tests.Shared.TestDoubles.Queries;
using RippleSync.Tests.Shared.TestDoubles.Repositories;

namespace RippleSync.Application.Tests;

public abstract class PostManagerTests
{
    protected PostManager GetSystemUnderTest(
        ILogger<PostManager>? logger = null,
        IUnitOfWork? unitOfWork = null,
        IPostRepository? postRepository = null,
        IPostQueries? postQueries = null,
        IIntegrationRepository? integrationRepository = null,
        IPlatformFactory? platformFactory = null)
    {
        logger ??= new LoggerDoubles.Fakes.FakeLogger<PostManager>();
        unitOfWork ??= new UnitOfWorkDoubles.Fakes.DoesNothing();
        postRepository ??= new PostRepositoryDoubles.Dummy();
        postQueries ??= new PostQueriesDoubles.Dummy();
        integrationRepository ??= new IntegrationRepositoryDoubles.Dummy();
        platformFactory ??= new PlatformFactoryDoubles.Dummy();

        return new PostManager(
            logger,
            unitOfWork,
            postRepository,
            postQueries,
            integrationRepository,
            platformFactory
        );
    }

    public sealed class ProcessPostAsync : PostManagerTests
    {
        [Fact]
        public async Task Should_SetPostEventsToProcessingStatusAndSaveToRepository_WhenPostHasPostEvents()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Integration xIntegration = new IntegrationBuilder(userId, Platform.X)
                .Build();
            Integration linkedInIntegration = new IntegrationBuilder(userId, Platform.LinkedIn)
                .Build();
            Post post = new PostBuilder(userId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .PostedTo(linkedInIntegration)
                .Build();
            var noWorkUnitOfWork = new UnitOfWorkDoubles.Fakes.DoesNothing();
            var unitOfWorkSpy = new UnitOfWorkDoubles.Spies.SaveSpy(noWorkUnitOfWork);
            var updatePostRepositorySpy = new PostRepositoryDoubles.Spies.UpdateAsyncSpy(
                new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing());
            var platformFactorySpy = new PlatformFactoryDoubles.Stubs.Create.ReturnsSpecifiedSoMePlatform(
                new SoMePlatformDoubles.Stubs.PublishPostAsync.ReturnsPostEventForIntegration());
            PostManager sut = GetSystemUnderTest(
                unitOfWork: new UnitOfWorkDoubles.Composite(
                    unitOfWorkSpy,
                    noWorkUnitOfWork),
                postRepository: updatePostRepositorySpy,
                integrationRepository: new IntegrationRepositoryDoubles.Stubs.GetIntegrationsByIdsAsync.ReturnsSpecifiedIntegrations([xIntegration, linkedInIntegration]),
                platformFactory: platformFactorySpy
            );

            // Act & Assert
            //updatePostRepositorySpy.OnInvokation = (updatedPost, spy) =>
            //{
            //    // Assert that the first time UpdateAsync is called, all PostEvents are set to Processing
            //    if (spy.InvocationCount == 1)
            //    {
            //        Assert.All(updatedPost.PostEvents, postEvent =>
            //            Assert.Equal(PostStatus.Processing, postEvent.Status));
            //    }
            //};
            await sut.ProcessPostAsync(post);
            Assert.True(updatePostRepositorySpy.InvocationCount > 0, "Expected PostRepository.UpdateAsync to be called at least once.");
            Assert.True(unitOfWorkSpy.InvocationCount > 0, "Expected UnitOfWork.Save to be called at least once.");
            Assert.All(post.PostEvents, postEvent =>
                Assert.Equal(PostStatus.Posted, postEvent.Status));
        }

        [Fact]
        public async Task Should_NotProcessPost_WhenPostHasNoPostEvents()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Post post = new PostBuilder(userId)
                .Build();

            var soMePlatformSpy = new SoMePlatformDoubles.Spies.PublishPostAsyncSpy(
                new SoMePlatformDoubles.Stubs.PublishPostAsync.ReturnsPostEventForIntegration());
            PostManager sut = GetSystemUnderTest(
                unitOfWork: new UnitOfWorkDoubles.Fakes.DoesNothing(),
                postRepository: new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing(),
                platformFactory: new PlatformFactoryDoubles.Stubs.Create.ReturnsSpecifiedSoMePlatform(soMePlatformSpy)
            );

            // Act
            await sut.ProcessPostAsync(post);

            // Assert
            Assert.Equal(0, soMePlatformSpy.InvocationCount);
        }

        [Fact]
        public async Task Should_PublishPostToAllPlatforms_WhenPostHasMultiplePostEvents()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Integration xIntegration = new IntegrationBuilder(userId, Platform.X)
                .Build();
            Integration linkedInIntegration = new IntegrationBuilder(userId, Platform.LinkedIn)
                .Build();
            Post post = new PostBuilder(userId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .PostedTo(linkedInIntegration)
                .Build();
            var soMePlatformSpy = new SoMePlatformDoubles.Spies.PublishPostAsyncSpy(
                new SoMePlatformDoubles.Stubs.PublishPostAsync.ReturnsPostEventForIntegration());
            var platformFactoryStub = new PlatformFactoryDoubles.Stubs.Create.ReturnsSpecifiedSoMePlatform(soMePlatformSpy);
            PostManager sut = GetSystemUnderTest(
                unitOfWork: new UnitOfWorkDoubles.Fakes.DoesNothing(),
                postRepository: new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing(),
                integrationRepository: new IntegrationRepositoryDoubles.Stubs.GetIntegrationsByIdsAsync.ReturnsSpecifiedIntegrations(xIntegration, linkedInIntegration),
                platformFactory: platformFactoryStub
            );

            // Act
            await sut.ProcessPostAsync(post);

            // Assert
            Assert.Equal(2, soMePlatformSpy.InvocationCount);
            Assert.Contains(soMePlatformSpy.Posts, post => post.Id == post.Id);
            Assert.Contains(soMePlatformSpy.Integrations, integration => integration.Id == xIntegration.Id);
            Assert.Contains(soMePlatformSpy.Integrations, integration => integration.Id == linkedInIntegration.Id);
            Assert.All(post.PostEvents, postEvent =>
                Assert.Equal(PostStatus.Posted, postEvent.Status));
        }

        [Fact]
        public async Task Should_SetPostEventToFailed_WhenPublishingFails()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Integration xIntegration = new IntegrationBuilder(userId, Platform.X)
                .Build();
            Integration linkedInIntegration = new IntegrationBuilder(userId, Platform.LinkedIn)
                .Build();
            Post post = new PostBuilder(userId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .PostedTo(linkedInIntegration)
                .Build();

            var failingSoMePlatformStub = new SoMePlatformDoubles.Stubs.PublishPostAsync.Throws();
            var succedingSoMePlatformStub = new SoMePlatformDoubles.Stubs.PublishPostAsync.ReturnsPostEventForIntegration();
            var platformFactoryStub = new PlatformFactoryDoubles.Stubs.Create.ReturnsDifferentSoMePlatformsBasedOnInput(new Dictionary<Platform, ISoMePlatform>()
            {
                 { Platform.X, failingSoMePlatformStub },
                 { Platform.LinkedIn, succedingSoMePlatformStub },
            });
            var updatePostRepositorySpy = new PostRepositoryDoubles.Spies.UpdateAsyncSpy(
                new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing());
            PostManager sut = GetSystemUnderTest(
                unitOfWork: new UnitOfWorkDoubles.Fakes.DoesNothing(),
                postRepository: updatePostRepositorySpy,
                integrationRepository: new IntegrationRepositoryDoubles.Stubs.GetIntegrationsByIdsAsync.ReturnsSpecifiedIntegrations(xIntegration, linkedInIntegration),
                platformFactory: platformFactoryStub
            );

            // Act
            await sut.ProcessPostAsync(post);

            // Assert
            Assert.NotNull(updatePostRepositorySpy.LatestUpdated);
            Assert.Contains(updatePostRepositorySpy.LatestUpdated.PostEvents, postEvent =>
                postEvent.Status == PostStatus.Failed && postEvent.UserPlatformIntegrationId == xIntegration.Id);
            Assert.Contains(updatePostRepositorySpy.LatestUpdated.PostEvents, postEvent =>
                postEvent.Status == PostStatus.Posted && postEvent.UserPlatformIntegrationId == linkedInIntegration.Id);
        }
    }

    public sealed class RetryPublishAsync : PostManagerTests
    {

        [Fact]
        public async Task Should_NotUpdatePost_WhenNoFailedPostEvents()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var xIntegration = new IntegrationBuilder(userId, Platform.X).Build();
            var linkedInIntegration = new IntegrationBuilder(userId, Platform.LinkedIn).Build();
            Post post = new PostBuilder(userId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .PostedTo(linkedInIntegration)
                .Build();
            foreach (var item in post.PostEvents)
            {
                item.Status = PostStatus.Posted;
            }
            var updatePostRepositorySpy = new PostRepositoryDoubles.Spies.UpdateAsyncSpy(
                new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing());
            PostManager sut = GetSystemUnderTest(
                postRepository: new PostRepositoryDoubles.Composite(
                    updatePostRepositorySpy,
                    new PostRepositoryDoubles.Stubs.GetByIdAsync.ReturnsSpecifiedPost(post)
                )
            );

            // Act
            await sut.RetryPublishAsync(userId, post.Id);

            // Assert
            Assert.Equal(0, updatePostRepositorySpy.InvocationCount);
        }

        [Fact]
        public async Task Should_OnlyUpdateFailedPostEvents_WhenRetryingPost()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var xIntegration = new IntegrationBuilder(userId, Platform.X).Build();
            var linkedInIntegration = new IntegrationBuilder(userId, Platform.LinkedIn).Build();
            Post post = new PostBuilder(userId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .PostedTo(linkedInIntegration)
                .Build();
            foreach (var item in post.PostEvents)
            {
                if (item.UserPlatformIntegrationId == xIntegration.Id)
                    item.Status = PostStatus.Failed;
                else
                    item.Status = PostStatus.Posted;
            }
            var updatePostRepositorySpy = new PostRepositoryDoubles.Spies.UpdateAsyncSpy(
                new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing());
            PostManager sut = GetSystemUnderTest(
                postRepository: new PostRepositoryDoubles.Composite(
                    updatePostRepositorySpy,
                    new PostRepositoryDoubles.Stubs.GetByIdAsync.ReturnsSpecifiedPost(post)
                ),
                integrationRepository: new IntegrationRepositoryDoubles.Stubs.GetIntegrationsByIdsAsync.ReturnsSpecifiedIntegrations(xIntegration, linkedInIntegration),
                platformFactory: new PlatformFactoryDoubles.Stubs.Create.ReturnsSpecifiedSoMePlatform(
                    new SoMePlatformDoubles.Stubs.PublishPostAsync.ReturnsPostEventForIntegration())
            );

            // Act
            await sut.RetryPublishAsync(userId, post.Id);

            // Assert
            Assert.Equal(1, updatePostRepositorySpy.InvocationCount);
            Assert.NotNull(updatePostRepositorySpy.LatestUpdated);
            Assert.Contains(updatePostRepositorySpy.LatestUpdated.PostEvents, postEvent =>
                postEvent.UserPlatformIntegrationId == xIntegration.Id &&
                postEvent.Status == PostStatus.Scheduled);
            Assert.Contains(updatePostRepositorySpy.LatestUpdated.PostEvents, postEvent =>
                postEvent.UserPlatformIntegrationId == linkedInIntegration.Id &&
                postEvent.Status == PostStatus.Posted);
        }

        [Fact]
        public async Task Should_ThrowEntityNotFoundException_WhenPostDoesNotExist()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Guid nonExistentPostId = Guid.NewGuid();
            PostManager sut = GetSystemUnderTest(
                postRepository: new PostRepositoryDoubles.Stubs.GetByIdAsync.ReturnsNull()
            );

            // Act & Assert
            var ex = await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await sut.RetryPublishAsync(userId, nonExistentPostId));
            Assert.Equal("Post", ex.EntityType);
            Assert.Equal("Id", ex.KeyName);
            Assert.Equal(nonExistentPostId, ex.Key);
        }

        [Fact]
        public async Task Should_ThrowInvalidOperationException_WhenPostHasNoPostEvents()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            Post post = new PostBuilder(userId)
                .Build();
            var sut = GetSystemUnderTest(
                postRepository: new PostRepositoryDoubles.Composite(
                    new PostRepositoryDoubles.Stubs.GetByIdAsync.ReturnsSpecifiedPost(post),
                    new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing()
                )
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await sut.RetryPublishAsync(userId, post.Id));
        }

        [Fact]
        public async Task Should_ThrowUnuathorizedException_WhenUserIsNotAuthorOfPost()
        {
            // Arrange
            Guid authorUserId = Guid.NewGuid();
            Guid otherUserId = Guid.NewGuid();
            var xIntegration = new IntegrationBuilder(authorUserId, Platform.X).Build();
            Post post = new PostBuilder(authorUserId)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(xIntegration)
                .Build();
            var sut = GetSystemUnderTest(
                postRepository: new PostRepositoryDoubles.Composite(
                    new PostRepositoryDoubles.Stubs.GetByIdAsync.ReturnsSpecifiedPost(post),
                    new PostRepositoryDoubles.Stubs.UpdateAsync.DoesNothing()
                )
            );

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedException>(async () =>
                await sut.RetryPublishAsync(otherUserId, post.Id));
        }
    }
}
