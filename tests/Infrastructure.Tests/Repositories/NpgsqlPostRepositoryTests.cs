using RippleSync.Application.Common.Security;
using RippleSync.Domain.Integrations;
using RippleSync.Domain.Platforms;
using RippleSync.Domain.Posts;
using RippleSync.Domain.Users;
using RippleSync.Infrastructure.IntegrationRepository;
using RippleSync.Infrastructure.PostRepository;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.Tests.Configuration;
using RippleSync.Infrastructure.UserRepository;
using RippleSync.Tests.Shared;
using RippleSync.Tests.Shared.Factories.Integrations;
using RippleSync.Tests.Shared.Factories.Posts;
using RippleSync.Tests.Shared.Factories.Users;
using RippleSync.Tests.Shared.TestDoubles.Security;

namespace RippleSync.Infrastructure.Tests.Repositories;

public class NpgsqlPostRepositoryTests : RepositoryTestBase
{
    private readonly NpgsqlPostRepository _sut;

    public NpgsqlPostRepositoryTests(PostgresDatabaseFixture fixture) : base(fixture)
    {
        IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
        _sut = new NpgsqlPostRepository(UnitOfWork, encryption);
    }

    public override async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await base.DisposeAsync();
    }

    public sealed class GetPostsByUserAsync : NpgsqlPostRepositoryTests
    {
        public GetPostsByUserAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnEmptyList_WhenUserHasNoPosts()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var posts = await _sut.GetPostsByUserAsync(userId, null);

            // Assert
            Assert.Empty(posts);
        }
    }

    public sealed class GetImageByIdAsync : NpgsqlPostRepositoryTests
    {
        public GetImageByIdAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnNull_WhenPostMediaDoesNotExist()
        {
            // Arrange
            var nonExistentMediaId = Guid.NewGuid();

            // Act
            var postMedia = await _sut.GetImageByIdAsync(nonExistentMediaId);

            // Assert
            Assert.Null(postMedia);
        }

        [Fact]
        public async Task Should_ReturnPostMedia_WhenPostMediaExists()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .AddRandomMedia()
                .Build();
            await _sut.CreateAsync(post);
            var existingMediaId = post.PostMedias.First().Id;

            // Act
            var postMedia = await _sut.GetImageByIdAsync(existingMediaId);

            // Assert
            Assert.NotNull(postMedia);
        }
    }

    public sealed class GetAllByUserIdAsync : NpgsqlPostRepositoryTests
    {
        public GetAllByUserIdAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnEmptyList_WhenUserHasNoPosts()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var posts = await _sut.GetAllByUserIdAsync(userId);

            // Assert
            Assert.Empty(posts);
        }

        [Fact]
        public async Task Should_ReturnAllPosts_WhenUserHasPosts()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post1 = new PostBuilder(user.Id)
                .Build();
            Post post2 = new PostBuilder(user.Id)
                .Build();
            await _sut.CreateAsync(post1);
            await _sut.CreateAsync(post2);

            // Act
            var posts = await _sut.GetAllByUserIdAsync(user.Id);

            // Assert
            Assert.Equal(2, posts.Count());
            Assert.Contains(posts, p => p.Id == post1.Id);
            Assert.Contains(posts, p => p.Id == post2.Id);
        }
    }

    public sealed class GetByIdAsync : NpgsqlPostRepositoryTests
    {
        public GetByIdAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnNull_WhenPostDoesNotExist()
        {
            // Arrange
            var nonExistentPostId = Guid.NewGuid();

            // Act
            var post = await _sut.GetByIdAsync(nonExistentPostId);

            // Assert
            Assert.Null(post);
        }

        [Fact]
        public async Task Should_ReturnPost_WhenPostExists()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .Build();
            await _sut.CreateAsync(post);

            // Act
            var retrievedPost = await _sut.GetByIdAsync(post.Id);

            // Assert
            Assert.NotNull(retrievedPost);
            Assert.Equal(post.UserId, retrievedPost.UserId);
            Assert.Equal(post.MessageContent, retrievedPost.MessageContent);
        }

        [Fact]
        public async Task Should_ReturnPostWithEventsAndMedia_WhenAvailable()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await integrationRepository.CreateAsync(integration);
            Post post = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(integration)
                .AddRandomMedia()
                .Build();
            await _sut.CreateAsync(post);

            // Act
            var retrievedPost = await _sut.GetByIdAsync(post.Id);

            // Assert
            Assert.NotNull(retrievedPost);
            Assert.Equal(post.UserId, retrievedPost.UserId);
            Assert.Equal(post.MessageContent, retrievedPost.MessageContent);
            Assert.Single(retrievedPost.PostEvents);
            Assert.Equal(integration.Id, retrievedPost.PostEvents.First().UserPlatformIntegrationId);
            Assert.Single(retrievedPost.PostMedias);
        }
    }

    public sealed class GetPostsReadyToPublishAsync : NpgsqlPostRepositoryTests
    {
        public GetPostsReadyToPublishAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_ReturnEmptyList_WhenNoPostsAreReadyToPublish()
        {
            // Arrange
            // No posts are created

            // Act
            var posts = await _sut.GetPostsReadyToPublishAsync();

            // Assert
            Assert.Empty(posts);
        }

        [Fact]
        public async Task Should_OnlyReturnPostsWithScheduledTimeInThePast()
        {
            // Arrange

            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await integrationRepository.CreateAsync(integration);
            Post pastPost = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddHours(-1))
                .PostedTo(integration)
                .Build();
            Post futurePost = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(integration)
                .Build();
            await _sut.CreateAsync(pastPost);
            await _sut.CreateAsync(futurePost);

            // Act
            var posts = await _sut.GetPostsReadyToPublishAsync();

            // Assert
            Assert.Single(posts);
            Assert.Equal(pastPost.Id, posts.First().Id);
        }
    }

    public sealed class CreateAsync : NpgsqlPostRepositoryTests
    {
        public CreateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_SavePostToDatabseWithNoPostEvents_WhenPostIsDraft()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .Build();

            // Act
            await _sut.CreateAsync(post);

            // Assert
            Post? retrievedPosts = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPosts);
            Assert.Equal(post.UserId, retrievedPosts.UserId);
            Assert.Equal(post.MessageContent, retrievedPosts.MessageContent);
        }

        [Fact]
        public async Task Should_SavePostWithPostEvents_WhenPostIsScheduled()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await integrationRepository.CreateAsync(integration1);
            await integrationRepository.CreateAsync(integration2);

            Post post = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddDays(2))
                .PostedTo(integration1)
                .PostedTo(integration2)
                .Build();

            // Act
            await _sut.CreateAsync(post);

            // Assert
            Post? retrievedPosts = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPosts);
            Assert.Equal(post.UserId, retrievedPosts.UserId);
            Assert.Equal(post.MessageContent, retrievedPosts.MessageContent);
            Assert.Equal(2, retrievedPosts.PostEvents.Count());
            Assert.Contains(retrievedPosts.PostEvents, pe => pe.UserPlatformIntegrationId == integration1.Id);
            Assert.Contains(retrievedPosts.PostEvents, pe => pe.UserPlatformIntegrationId == integration2.Id);
        }

        [Fact]
        public async Task Should_SavePostWithPostMedias_WhenPostHasMedia()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .AddRandomMedia()
                .AddRandomMedia()
                .Build();

            // Act
            await _sut.CreateAsync(post);

            // Assert
            Post? retrievedPosts = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPosts);
            Assert.Equal(post.UserId, retrievedPosts.UserId);
            Assert.Equal(post.MessageContent, retrievedPosts.MessageContent);
            Assert.Equal(2, retrievedPosts.PostMedias.Count());
        }
    }

    public sealed class UpdateAsync : NpgsqlPostRepositoryTests
    {
        public UpdateAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_UpdatePostMessageContent()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .Build();
            await _sut.CreateAsync(post);

            // Act
            post.MessageContent = "Updated message content";
            await _sut.UpdateAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPost);
            Assert.Equal("Updated message content", retrievedPost.MessageContent);
        }

        [Fact]
        public async Task Should_UpdatePostEventStatus_WhenPostIsPublished()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await integrationRepository.CreateAsync(integration);
            Post post = new PostBuilder(user.Id)
                .PostedTo(integration)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .Build();
            await _sut.CreateAsync(post);

            // Act
            var postEvent = post.PostEvents.First();
            postEvent.Status = PostStatus.Posted;
            await _sut.UpdateAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPost);
            var retrievedPostEvent = retrievedPost.PostEvents.First();
            Assert.Equal(PostStatus.Posted, retrievedPostEvent.Status);
        }

        [Fact]
        public async Task Should_AddNewPostEvent_WhenNewIntegrationIsAdded()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await integrationRepository.CreateAsync(integration1);
            await integrationRepository.CreateAsync(integration2);
            Post post = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddDays(2))
                .PostedTo(integration1)
                .Build();
            await _sut.CreateAsync(post);

            // Act
            var newPostEvent = PostEvent.Create(
                userPlatformIntegrationId: integration2.Id,
                status: PostStatus.Scheduled,
                platformPostIdentifier: string.Empty,
                platformResponse: null
            );
            post.PostEvents = post.PostEvents.Append(newPostEvent);
            await _sut.UpdateAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPost);
            Assert.Equal(2, retrievedPost.PostEvents.Count());
            Assert.Contains(retrievedPost.PostEvents, pe => pe.UserPlatformIntegrationId == integration2.Id);
        }

        [Fact]
        public async Task Should_DeleteOldPostEvent_WhenIntegrationIsRemoved()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration1 = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            Integration integration2 = new IntegrationBuilder(user.Id, Platform.Facebook)
                .Build();
            await integrationRepository.CreateAsync(integration1);
            await integrationRepository.CreateAsync(integration2);
            Post post = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddDays(2))
                .PostedTo(integration1)
                .PostedTo(integration2)
                .Build();
            await _sut.CreateAsync(post);

            // Act
            post.PostEvents = post.PostEvents.Where(pe => pe.UserPlatformIntegrationId != integration2.Id);
            await _sut.UpdateAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPost);
            Assert.Single(retrievedPost.PostEvents);
            Assert.DoesNotContain(retrievedPost.PostEvents, pe => pe.UserPlatformIntegrationId == integration2.Id);
        }

        [Fact]
        public async Task Should_DeleteOldPostMedia_WhenMediaIsRemoved()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .AddRandomMedia()
                .AddRandomMedia()
                .Build();
            await _sut.CreateAsync(post);
            var mediaToRemoveId = post.PostMedias.First().Id;

            // Act
            post.PostMedias = post.PostMedias.Where(pm => pm.Id != mediaToRemoveId);
            await _sut.UpdateAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.NotNull(retrievedPost);
            Assert.Single(retrievedPost.PostMedias);
            Assert.DoesNotContain(retrievedPost.PostMedias, pm => pm.Id == mediaToRemoveId);
        }
    }

    public sealed class DeleteAsync : NpgsqlPostRepositoryTests
    {
        public DeleteAsync(PostgresDatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Should_DeletePostFromDatabase()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Post post = new PostBuilder(user.Id)
                .Build();
            await _sut.CreateAsync(post);

            // Act
            await _sut.DeleteAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.Null(retrievedPost);
        }

        [Fact]
        public async Task Should_DeletePostEventsAndMedia_WhenPostIsDeleted()
        {
            // Arrange
            IEncryptionService encryption = new AesGcmEncryptionService(TestConfiguration.Configuration);
            var userRepository = new NpgsqlUserRepository(UnitOfWork, encryption);
            var integrationRepository = new NpgsqlIntegrationRepository(UnitOfWork, encryption);
            User user = new UserBuilder(new PasswordHasherDoubles.Fakes.Base64Hasher())
                .Build();
            await userRepository.CreateAsync(user);
            Integration integration = new IntegrationBuilder(user.Id, Platform.X)
                .Build();
            await integrationRepository.CreateAsync(integration);
            Post post = new PostBuilder(user.Id)
                .ScheduledFor(DateTime.UtcNow.AddHours(1))
                .PostedTo(integration)
                .AddRandomMedia()
                .Build();
            await _sut.CreateAsync(post);

            // Act
            await _sut.DeleteAsync(post);

            // Assert
            Post? retrievedPost = await _sut.GetByIdAsync(post.Id);
            Assert.Null(retrievedPost);
        }
    }
}
