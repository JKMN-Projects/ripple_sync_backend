using Dapper;
using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.PostRepository.Entities;
using System.Data;
using System.Threading.Channels;

namespace RippleSync.Infrastructure.PostRepository;

internal class DapperPostRepository(
    IUnitOfWork uow,
    IEncryptionService encryptor) : BaseRepository(uow), IPostRepository, IPostQueries
{
    /// Enten kan man lave hårdt kodet mappers
    //public class GetPostsByUserResponseEntityMap : EntityMap<GetPostsByUserResponseEntity>
    //{
    //    public GetPostsByUserResponseEntityMap()
    //    {
    //        Map(p => p.Id).ToColumn("id");
    //        Map(p => p.MessageContent).ToColumn("message_content");
    //        Map(p => p.MediaIds).ToColumn("media_ids");
    //        Map(p => p.StatusName).ToColumn("status_name");
    //        Map(p => p.Timestamp).ToColumn("timestamp");
    //        Map(p => p.Platforms).ToColumn("platforms");
    //    }
    //}
    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {
        ///Eller fylde sine queries med alias. 
        ///Aldrig været stor fan af at en ORM ændrer mine objecter, selvom jeg elsker dappers mapping når det er opsat
        string getPostSummaryQuery = @"
            SELECT
                p.id AS Id,
                p.message_content AS MessageContent,
                COALESCE(
                    (SELECT ARRAY_AGG(pm.id) 
                     FROM post_media AS pm 
                     WHERE pm.post_id = p.id),
                    '{}'
                ) AS MediaIds,
                pe_data.status_name AS StatusName,
                pe_data.timestamp AS Timestamp,
                pe_data.platforms AS Platforms
            FROM post AS p
            LEFT JOIN LATERAL (
                SELECT 
                    (SELECT ps.status 
                     FROM post_event AS pe 
                     JOIN post_status AS ps ON pe.post_status_id = ps.id 
                     WHERE pe.post_id = p.id 
                     ORDER BY ps.id DESC 
                     LIMIT 1) AS status_name,
                    EXTRACT(EPOCH FROM p.scheduled_for)::bigint * 1000 AS timestamp,
                    (SELECT ARRAY_AGG(pl.platform_name) 
                     FROM post_event AS pe
                     JOIN user_platform_integration AS upi ON pe.user_platform_integration_id = upi.id
                     JOIN platform AS pl ON upi.platform_id = pl.id
                     WHERE pe.post_id = p.id) AS platforms
                WHERE EXISTS (SELECT 1 FROM post_event WHERE post_id = p.id)
            ) AS pe_data ON true
            WHERE p.user_account_id = @UserId";

        if (status != null)
        {
            if (status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            {
                getPostSummaryQuery += " AND pe_data.status_name IS NULL";
            }
            else
            {
                getPostSummaryQuery += " AND pe_data.status_name = @Status";
            }
        }

        getPostSummaryQuery += " ORDER BY p.submitted_at DESC";

        var userPostEntities = await Connection.QueryAsync<GetPostsByUserResponseEntity>(
            getPostSummaryQuery,
            new { UserId = userId, Status = status },
            Transaction);

        return userPostEntities.Any()
            ? userPostEntities.Select(up => new GetPostsByUserResponse(
                up.Id,
                DecryptPostMessage(up.MessageContent),
                up.MediaIds,
                string.IsNullOrWhiteSpace(up.StatusName) ? PostStatus.Draft.ToString() : up.StatusName,
                up.Timestamp,
                up.Platforms))
            : [];
    }

    public async Task<string?> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        const string query = "SELECT * FROM post_media WHERE id = @Id LIMIT 1";

        var postMediaEntity = await Connection.QuerySingleOrDefaultAsync<PostMediaEntity>(
            query,
            new { Id = imageId },
            Transaction);

        return postMediaEntity != null
            ? DecryptPostMedia(postMediaEntity.ImageData)
            : null;
    }

    public async Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string query = "SELECT * FROM post WHERE user_account_id = @UserId";

        var postEntities = await Connection.QueryAsync<PostEntity>(
            query,
            new { UserId = userId },
            Transaction);

        if (!postEntities.Any()) return [];

        return await GetMediaAndEventsForPosts(postEntities, cancellationToken);
    }

    public async Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        const string postQuery = "SELECT * FROM post WHERE id = @PostId LIMIT 1";
        const string mediaQuery = "SELECT * FROM post_media WHERE post_id = @PostId";
        const string eventsQuery = "SELECT * FROM post_event WHERE post_id = @PostId";

        var postIdParam = new { PostId = postId };

        var postEntity = await Connection.QuerySingleOrDefaultAsync<PostEntity>(
            postQuery,
            postIdParam,
            Transaction);

        if (postEntity == null) return null;

        var postMedias = await Connection.QueryAsync<PostMediaEntity>(
            mediaQuery,
            postIdParam,
            Transaction);

        var postEvents = await Connection.QueryAsync<PostEventEntity>(
            eventsQuery,
            postIdParam,
            Transaction);

        return Post.Reconstitute(
            postEntity.Id,
            postEntity.UserAccountId,
            DecryptPostMessage(postEntity.MessageContent),
            postEntity.SubmittedAt,
            postEntity.UpdatedAt,
            postEntity.ScheduledFor,
            postMedias.Select(pme => PostMedia.Reconstitute(pme.Id, pme.ImageData)),
            postEvents.Select(pee => PostEvent.Reconstitute(
                pee.UserPlatformIntegrationId,
                (PostStatus)pee.PostStatusId,
                pee.PlatformPostIdentifier,
                pee.PlatformResponse))
        );
    }

    public async Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default)
    {
        const string getReadyToPostAndClaimPostsSql = @"
            WITH to_claim AS (
                SELECT pe.post_id, pe.user_platform_integration_id
                FROM post_event AS pe
                JOIN post AS p 
                    ON p.id = pe.post_id
                WHERE pe.post_status_id = @ScheduledId
                  AND p.scheduled_for < NOW()
                ORDER BY p.scheduled_for ASC
                LIMIT 1000
                FOR UPDATE SKIP LOCKED
            ),
            updated AS (
                UPDATE post_event AS pe
                SET post_status_id = @ProcessingId
                FROM to_claim
                JOIN post AS p 
                    ON p.id = to_claim.post_id
                WHERE pe.post_id = to_claim.post_id
                  AND pe.user_platform_integration_id = to_claim.user_platform_integration_id
                RETURNING p.*
            )
            SELECT DISTINCT * FROM updated;";

        var postEntities = await Connection.QueryAsync<PostEntity>(
            getReadyToPostAndClaimPostsSql,
            new
            {
                ScheduledId = (int)PostStatus.Scheduled,
                ProcessingId = (int)PostStatus.Processing
            },
            Transaction);

        if (!postEntities.Any()) return [];

        return await GetMediaAndEventsForPosts(postEntities, cancellationToken);
    }

    public async Task CreateAsync(Post post, CancellationToken cancellationToken = default)
    {
        const string postInsert = @"
            INSERT INTO post (id, user_account_id, message_content, submitted_at, updated_at, scheduled_for)
            VALUES (@Id, @UserAccountId, @MessageContent, @SubmittedAt, @UpdatedAt, @ScheduledFor)";

        const string mediaInsert = @"
            INSERT INTO post_media (id, post_id, image_data)
            VALUES (@Id, @PostId, @ImageData)";

        const string eventInsert = @"
            INSERT INTO post_event (post_id, user_platform_integration_id, post_status_id, platform_post_identifier, platform_response)
            VALUES (@PostId, @UserPlatformIntegrationId, @PostStatusId, @PlatformPostIdentifier, @PlatformResponse)";

        var postEntity = new PostEntity(
            post.Id,
            post.UserId,
            EncryptPostMessage(post.MessageContent),
            post.SubmittedAt,
            post.UpdatedAt,
            post.ScheduledFor);

        int rowsAffected = await Connection.ExecuteAsync(postInsert, postEntity, Transaction);

        if (rowsAffected <= 0)
            throw new InvalidOperationException("No rows were affected on post insert");

        if (post.PostMedia?.Any() == true)
        {
            var postMediasEntities = post.PostMedia.Select(pm => new PostMediaEntity(
                pm.Id,
                post.Id,
                EncryptPostMedia(pm.ImageData)));

            rowsAffected = await Connection.ExecuteAsync(mediaInsert, postMediasEntities, Transaction);

            if (rowsAffected <= 0)
                throw new InvalidOperationException("No rows were affected on PostMedias insert");
        }

        if (post.PostEvents?.Any() == true)
        {
            var postEventsEntities = post.PostEvents.Select(pe => new PostEventEntity(
                post.Id,
                pe.UserPlatformIntegrationId,
                (int)pe.Status,
                pe.PlatformPostIdentifier,
                pe.PlatformResponse?.ToString()));

            rowsAffected = await Connection.ExecuteAsync(eventInsert, postEventsEntities, Transaction);

            if (rowsAffected <= 0)
                throw new InvalidOperationException("No rows were affected on PostEvent insert");
        }
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        const string postUpdate = @"
            UPDATE post 
            SET user_account_id = @UserAccountId, 
                message_content = @MessageContent, 
                submitted_at = @SubmittedAt, 
                updated_at = @UpdatedAt, 
                scheduled_for = @ScheduledFor
            WHERE id = @Id";

        const string mediaDelete = "DELETE FROM post_media WHERE post_id = @PostId";
        const string mediaInsert = @"
            INSERT INTO post_media (id, post_id, image_data)
            VALUES (@Id, @PostId, @ImageData)";

        const string eventDelete = "DELETE FROM post_event WHERE post_id = @PostId";
        const string eventInsert = @"
            INSERT INTO post_event (post_id, user_platform_integration_id, post_status_id, platform_post_identifier, platform_response)
            VALUES (@PostId, @UserPlatformIntegrationId, @PostStatusId, @PlatformPostIdentifier, @PlatformResponse)";

        var postEntity = new PostEntity(
            post.Id,
            post.UserId,
            EncryptPostMessage(post.MessageContent),
            post.SubmittedAt,
            post.UpdatedAt,
            post.ScheduledFor);

        int postRowsAffected = await Connection.ExecuteAsync(postUpdate, postEntity, Transaction);

        // Delete and re-insert pattern for child entities
        await Connection.ExecuteAsync(mediaDelete, new { PostId = post.Id }, Transaction);
        if (post.PostMedia?.Any() == true)
        {
            var postMediaEntities = post.PostMedia.Select(pm => new PostMediaEntity(
                pm.Id,
                post.Id,
                EncryptPostMedia(pm.ImageData)));
            await Connection.ExecuteAsync(mediaInsert, postMediaEntities, Transaction);
        }

        await Connection.ExecuteAsync(eventDelete, new { PostId = post.Id }, Transaction);
        if (post.PostEvents?.Any() == true)
        {
            var postEventEntities = post.PostEvents.Select(pe => new PostEventEntity(
                post.Id,
                pe.UserPlatformIntegrationId,
                (int)pe.Status,
                pe.PlatformPostIdentifier,
                pe.PlatformResponse?.ToString()));
            await Connection.ExecuteAsync(eventInsert, postEventEntities, Transaction);
        }

        if (postRowsAffected <= 0)
            throw new InvalidOperationException($"No rows were affected on Post update");
    }

    public async Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        const string deleteQuery = "DELETE FROM post WHERE id = @Id";

        int rowsAffected = await Connection.ExecuteAsync(
            deleteQuery,
            new { Id = post.Id },
            Transaction);

        if (rowsAffected <= 0)
            throw new InvalidOperationException("No rows were affected on Post remove");
    }

    private async Task<IEnumerable<Post>> GetMediaAndEventsForPosts(IEnumerable<PostEntity> postEntities, CancellationToken cancellationToken = default)
    {
        var postIds = postEntities.Select(p => p.Id).ToArray();

        const string mediaQuery = "SELECT * FROM post_media WHERE post_id = ANY(@PostIds)";
        const string eventsQuery = "SELECT * FROM post_event WHERE post_id = ANY(@PostIds)";

        var postMedias = await Connection.QueryAsync<PostMediaEntity>(
            mediaQuery,
            new { PostIds = postIds },
            Transaction);

        var postEvents = await Connection.QueryAsync<PostEventEntity>(
            eventsQuery,
            new { PostIds = postIds },
            Transaction);

        var mediaLookup = postMedias.GroupBy(pm => pm.PostId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());
        var eventLookup = postEvents.GroupBy(pe => pe.PostId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        return postEntities.Select(pe => Post.Reconstitute(
            pe.Id,
            pe.UserAccountId,
            DecryptPostMessage(pe.MessageContent),
            pe.SubmittedAt,
            pe.UpdatedAt,
            pe.ScheduledFor,
            mediaLookup.GetValueOrDefault(pe.Id, [])
                .Select(pme => PostMedia.Reconstitute(pme.Id, DecryptPostMedia(pme.ImageData))),
            eventLookup.GetValueOrDefault(pe.Id, [])
                .Select(pee => PostEvent.Reconstitute(
                    pee.UserPlatformIntegrationId,
                    (PostStatus)pee.PostStatusId,
                    pee.PlatformPostIdentifier,
                    pee.PlatformResponse))
        ));
    }

    public async Task RemoveScheduleOnAllPostsWithoutEventAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string deleteScheduleIfNoEventSql = @"
            UPDATE post 
            SET scheduled_for = NULL
            WHERE user_account_id = @UserId
              AND scheduled_for IS NOT NULL
              AND scheduled_for > NOW()
              AND NOT EXISTS (
                  SELECT 1 
                  FROM post_event 
                  WHERE post_event.post_id = post.id
              )";

        await Connection.ExecuteAsync(
            deleteScheduleIfNoEventSql,
            new { UserId = userId },
            Transaction);
    }

    private string EncryptPostMessage(string messageContent)
        => !string.IsNullOrWhiteSpace(messageContent)
            ? encryptor.Encrypt(EncryptionTask.PostMessageContent, messageContent)
            : string.Empty;

    private string DecryptPostMessage(string messageContent)
       => !string.IsNullOrWhiteSpace(messageContent)
            ? encryptor.Decrypt(EncryptionTask.PostMessageContent, messageContent)
            : string.Empty;

    private string EncryptPostMedia(string mediaContent)
        => !string.IsNullOrWhiteSpace(mediaContent)
            ? encryptor.Encrypt(EncryptionTask.PostMediaContent, mediaContent)
            : string.Empty;

    private string DecryptPostMedia(string mediaContent)
        => !string.IsNullOrWhiteSpace(mediaContent)
            ? encryptor.Decrypt(EncryptionTask.PostMediaContent, mediaContent)
            : string.Empty;
}