-- Critical for GetPostsReadyToPublishAsync
CREATE INDEX idx_post_event_scheduled_posts ON post_event (post_id, post_status_id) 
    WHERE post_status_id = 1; -- 'scheduled'	

-- Composite index for the ready-to-publish query
CREATE INDEX idx_post_scheduled_status ON post (scheduled_for, id)
	WHERE scheduled_for IS NOT NULL;

-- For post_event lookups by status 
CREATE INDEX idx_post_event_status ON post_event (post_status_id, post_id);

-- For the GetPostsByUserAsync status filter
CREATE INDEX idx_post_event_post_status ON post_event (post_id, post_status_id);