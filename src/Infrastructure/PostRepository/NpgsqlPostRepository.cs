using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.Security;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.PostRepository.Entities;

namespace RippleSync.Infrastructure.PostRepository;
internal class NpgsqlPostRepository(
    IUnitOfWork uow,
    IEncryptionService encryptor) : BaseRepository(uow), IPostRepository, IPostQueries
{
    public async Task<IEnumerable<GetPostsByUserResponse>> GetPostsByUserAsync(Guid userId, string? status, CancellationToken cancellationToken = default)
    {
        /// Get posts by user. 
        /// Get ID and Content everytime
        /// Media is a string of all found, own subquery
        /// StatusName, Timestamp and Platforms, are all dependent on an event being registered.
        ///     If not made, then all null, else, 
        ///         get schedule from post, 
        ///         status from postEvents highest values event, 
        ///         platforms through postEvent through userPlatfromIntegrations

        string getPostSummaryQuery = @"
            SELECT
                p.id,
                p.message_content,
                COALESCE(
                    (SELECT ARRAY_AGG(pm.id) 
                     FROM post_media AS pm 
                     WHERE pm.post_id = p.id),
                    '{}'
                ) AS media_ids,
                pe_data.status_name,
                pe_data.timestamp,
                pe_data.platforms
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

        IEnumerable<GetPostsByUserResponseEntity> userPostEntities = [];

        try
        {
            userPostEntities = await Connection.QueryAsync<GetPostsByUserResponseEntity>(getPostSummaryQuery, param: new { UserId = userId, Status = status }, trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userPostEntities.Any() ? userPostEntities.Select(up => new GetPostsByUserResponse(up.Id, DecryptPostMessage(up.MessageContent), up.MediaIds, string.IsNullOrWhiteSpace(up.StatusName) ? PostStatus.Draft.ToString() : up.StatusName, up.Timestamp, up.Platforms)) : [];
    }

    public async Task<string?> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        PostMediaEntity? postMediaEntity = null;

        try
        {
            postMediaEntity = await Connection.SelectSingleOrDefaultAsync<PostMediaEntity>("id = @Id", param: new { Id = imageId }, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return postMediaEntity != null
            ? DecryptPostMedia(postMediaEntity.ImageData)
            : null;
    }

    public async Task<IEnumerable<Post>> GetAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<Post> posts = [];

        try
        {
            var postEntities = await Connection.SelectAsync<PostEntity>("user_account_id = @UserId", param: new { UserId = userId }, ct: cancellationToken);

            if (!postEntities.Any()) return posts;

            posts = await GetMediaAndEventsForPosts(postEntities, cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return posts;
    }

    public async Task<Post?> GetByIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        try
        {
            var postIdParam = new { PostId = postId };

            var postEntity = await Connection.SelectSingleOrDefaultAsync<PostEntity>("id = @PostId", param: postIdParam, ct: cancellationToken);

            if (postEntity == null) return null;

            var postMedias = await Connection.SelectAsync<PostMediaEntity>("post_id = @PostId", param: postIdParam, ct: cancellationToken);
            var postEvents = await Connection.SelectAsync<PostEventEntity>("post_id = @PostId", param: postIdParam, ct: cancellationToken);

            return Post.Reconstitute(
                postEntity.Id,
                postEntity.UserAccountId,
                DecryptPostMessage(postEntity.MessageContent),
                postEntity.SubmittedAt,
                postEntity.UpdatedAt,
                postEntity.ScheduledFor,
                postMedias.Select(pme => PostMedia.Reconstitute(pme.Id, pme.ImageData)),
                postEvents.Select(pee => PostEvent.Reconstitute(pee.UserPlatformIntegrationId, (PostStatus)pee.PostStatusId, pee.PlatformPostIdentifier, pee.PlatformResponse))
            );
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return null;
    }

    public async Task<IEnumerable<Post>> GetPostsReadyToPublishAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Post> posts = [];

        //FOR UPDATE OF pe SKIP LOCKED skips rows currently locked in another transaction, potentially another consumer.
        const string getReadyToPostAndClaimPostsSql = @"
            WITH claimed_posts AS (
                SELECT pe.post_id
                FROM post AS p
                INNER JOIN post_event AS pe ON pe.post_id = p.id
                INNER JOIN post_status AS ps ON pe.post_status_id = ps.id
                WHERE p.scheduled_for IS NOT NULL
                    AND ps.status = 'scheduled'
                    AND p.scheduled_for < NOW()
                ORDER BY p.scheduled_for ASC
                LIMIT 1000
                FOR UPDATE OF pe SKIP LOCKED 
            )
            UPDATE post_event AS pe
            SET post_status_id = (SELECT id FROM post_status WHERE status = 'processing')
            FROM claimed_posts
            WHERE pe.post_id = claimed_posts.post_id
            RETURNING pe.post_id";

        try
        {
            var postIds = await Connection.QueryAsync<Guid>(
                    getReadyToPostAndClaimPostsSql,
                    trans: Transaction,
                    ct: cancellationToken
                );

            if (!postIds.Any())
                return [];

            var postEntities = await Connection.SelectAsync<PostEntity>("id = ANY(@PostIds)", param: new { PostIds = postIds }, ct: cancellationToken);

            if (!postEntities.Any()) return posts;

            posts = await GetMediaAndEventsForPosts(postEntities, cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return posts;
    }

    public async Task CreateAsync(Post post, CancellationToken cancellationToken = default)
    {
        var postEntity = new PostEntity(post.Id, post.UserId, EncryptPostMessage(post.MessageContent), post.SubmittedAt, post.UpdatedAt, post.ScheduledFor);

        try
        {
            int rowsAffected = await Connection.InsertAsync(postEntity, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on post insert");

            if (!post.PostMedia.NullOrEmpty())
            {
                var postMediasEntities = post.PostMedia.Select(pm => new PostMediaEntity(pm.Id, post.Id, EncryptPostMedia(pm.ImageData)));

                rowsAffected = await Connection.InsertAsync(postMediasEntities, trans: Transaction, ct: cancellationToken);

                if (rowsAffected <= 0)
                    throw new RepositoryException("No rows were affected on PostMedias insert");
            }

            if (!post.PostEvents.NullOrEmpty())
            {
                var postEventsEntities = post.PostEvents.Select(pe => new PostEventEntity(post.Id, pe.UserPlatformIntegrationId, (int)pe.Status, pe.PlatformPostIdentifier, pe.PlatformResponse?.ToString()));

                rowsAffected = await Connection.InsertAsync(postEventsEntities, trans: Transaction, ct: cancellationToken);

                if (rowsAffected <= 0)
                    throw new RepositoryException("No rows were affected on PostEvent insert");
            }
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        var postEntity = new PostEntity(post.Id, post.UserId, EncryptPostMessage(post.MessageContent), post.SubmittedAt, post.UpdatedAt, post.ScheduledFor);

        try
        {
            int postRowsAffected = await Connection.UpdateAsync(postEntity, trans: Transaction, ct: cancellationToken);

            var postMediaEntities = post.PostMedia.Select(pm => new PostMediaEntity(pm.Id, post.Id, EncryptPostMedia(pm.ImageData)));
            var postEventEntities = post.PostEvents.Select(pe => new PostEventEntity(post.Id, pe.UserPlatformIntegrationId, (int)pe.Status, pe.PlatformPostIdentifier, pe.PlatformResponse?.ToString()));

            int postMediaRowsAffected = await Connection.SyncAsync(postMediaEntities, parentIdentifiers: new { PostId = post.Id }, trans: Transaction, ct: cancellationToken);
            int postEventRowsAffected = await Connection.SyncAsync(postEventEntities, parentIdentifiers: new { PostId = post.Id }, trans: Transaction, ct: cancellationToken);

            if ((postRowsAffected + postMediaRowsAffected + postEventRowsAffected) <= 0)
                throw new RepositoryException($"No rows were affected on Post update");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    public async Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        var postEntity = new PostEntity(post.Id, post.UserId, post.MessageContent, post.SubmittedAt, post.UpdatedAt, post.ScheduledFor);

        try
        {
            int rowsAffected = await Connection.RemoveAsync(postEntity, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on Post remove");
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
    }

    private async Task<IEnumerable<Post>> GetMediaAndEventsForPosts(IEnumerable<PostEntity> postEntities, CancellationToken cancellationToken = default)
    {
        var postIdsParam = new { PostIds = postEntities.Select(p => p.Id).ToArray() };

        var postMedias = await Connection.SelectAsync<PostMediaEntity>(
            "post_id = ANY(@PostIds)",
            param: postIdsParam,
            ct: cancellationToken);

        var postEvents = await Connection.SelectAsync<PostEventEntity>(
            "post_id = ANY(@PostIds)",
            param: postIdsParam,
            ct: cancellationToken);

        var mediaLookup = postMedias.GroupBy(pm => pm.PostId).ToDictionary(g => g.Key, g => g.AsEnumerable());
        var eventLookup = postEvents.GroupBy(pe => pe.PostId).ToDictionary(g => g.Key, g => g.AsEnumerable());

        return postEntities.Select(pe => Post.Reconstitute(
            pe.Id,
            pe.UserAccountId,
            DecryptPostMessage(pe.MessageContent),
            pe.SubmittedAt,
            pe.UpdatedAt,
            pe.ScheduledFor,
            mediaLookup.GetValueOrDefault(pe.Id, []).Select(pme => PostMedia.Reconstitute(pme.Id, DecryptPostMedia(pme.ImageData))),
            eventLookup.GetValueOrDefault(pe.Id, []).Select(pee => PostEvent.Reconstitute(pee.UserPlatformIntegrationId, (PostStatus)pee.PostStatusId, pee.PlatformPostIdentifier, pee.PlatformResponse))
        ));
    }

    public async Task RemoveScheduleOnAllPostsWithoutEventAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        const string deleteSchefuleIfNoEventSql = @"
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

        try
        {
            int rowsAffected = await Connection.ExecuteAsync(deleteSchefuleIfNoEventSql, param: new { UserId = userId }, trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }
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
