using RippleSync.Application.Common.Queries;
using RippleSync.Application.Common.Repositories;
using RippleSync.Application.Common.UnitOfWork;
using RippleSync.Application.Posts;
using RippleSync.Domain.Posts;
using RippleSync.Infrastructure.Base;
using RippleSync.Infrastructure.JukmanORM.Exceptions;
using RippleSync.Infrastructure.JukmanORM.Extensions;
using RippleSync.Infrastructure.PostRepository.Entities;

namespace RippleSync.Infrastructure.PostRepository;
internal class NpgsqlPostRepository(IUnitOfWork uow) : BaseRepository(uow), IPostRepository, IPostQueries
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
            WHERE p.user_account_id = @UserId
            ORDER BY p.submitted_at DESC;";

        IEnumerable<GetPostsByUserResponseEntity> userPostEntities = [];

        try
        {
            userPostEntities = await Connection.QueryAsync<GetPostsByUserResponseEntity>(getPostSummaryQuery, param: new { UserId = userId }, trans: Transaction, ct: cancellationToken);
        }
        catch (Exception e)
        {
            ExceptionFactory.ThrowRepositoryException(GetType(), System.Reflection.MethodBase.GetCurrentMethod(), e);
        }

        return userPostEntities.Any() ? userPostEntities.Select(up => new GetPostsByUserResponse(up.Id, up.MessageContent, up.MediaIds, string.IsNullOrWhiteSpace(up.StatusName) ? PostStatus.Draft.ToString() : up.StatusName, up.Timestamp, up.Platforms)) : [];
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

        return postMediaEntity?.ImageData;
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
                postEntity.MessageContent,
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

        string getPostsToPublish = @"
            SELECT p.* 
	            FROM post AS p 
	            LEFT JOIN post_event AS pe 
		            ON pe.post_id = p.id
	            LEFT JOIN post_status as ps 
		            ON pe.post_status_id = ps.id
            WHERE p.scheduled_for IS NOT null
	            AND ps.status = 'scheduled'
                AND p.scheduled_for < now()
            ORDER BY p.scheduled_for ASC
            LIMIT 1000";

        try
        {
            var postEntities = await Connection.QueryAsync<PostEntity>(getPostsToPublish, trans: Transaction, ct: cancellationToken);

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
        var postEntity = new PostEntity(post.Id, post.UserId, post.MessageContent, post.SubmittedAt, post.UpdatedAt, post.ScheduledFor);

        try
        {
            int rowsAffected = await Connection.InsertAsync(postEntity, trans: Transaction, ct: cancellationToken);

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on post insert");

            if (!post.PostMedias.NullOrEmpty())
            {
                var postMediasEntities = post.PostMedias.Select(pm => new PostMediaEntity(pm.Id, post.Id, pm.ImageData));

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
        var postEntity = new PostEntity(post.Id, post.UserId, post.MessageContent, post.SubmittedAt, post.UpdatedAt, post.ScheduledFor);

        try
        {
            int rowsAffected = await Connection.UpdateAsync(postEntity, trans: Transaction, ct: cancellationToken);

            if (!post.PostMedias.NullOrEmpty())
            {
                var postMediaEntities = post.PostMedias.Select(pm => new PostMediaEntity(pm.Id, post.Id, pm.ImageData));

                rowsAffected += await Connection.SyncAsync(postMediaEntities, trans: Transaction, ct: cancellationToken);
            }

            if (!post.PostEvents.NullOrEmpty())
            {
                var postEventEntities = post.PostEvents.Select(pe => new PostEventEntity(post.Id, pe.UserPlatformIntegrationId, (int)pe.Status, pe.PlatformPostIdentifier, pe.PlatformResponse?.ToString()));

                rowsAffected += await Connection.SyncAsync(postEventEntities, trans: Transaction, ct: cancellationToken);
            }

            if (rowsAffected <= 0)
                throw new RepositoryException("No rows were affected on Post update");
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
            pe.MessageContent,
            pe.SubmittedAt,
            pe.UpdatedAt,
            pe.ScheduledFor,
            mediaLookup.GetValueOrDefault(pe.Id, []).Select(pme => PostMedia.Reconstitute(pme.Id, pme.ImageData)),
            eventLookup.GetValueOrDefault(pe.Id, []).Select(pee => PostEvent.Reconstitute(pee.UserPlatformIntegrationId, (PostStatus)pee.PostStatusId, pee.PlatformPostIdentifier, pee.PlatformResponse))
        ));
    }
}
