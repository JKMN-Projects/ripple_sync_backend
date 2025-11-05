ALTER TABLE post_media 
    DROP CONSTRAINT post_media_post_id_fkey,
    ADD CONSTRAINT post_media_post_id_fkey 
        FOREIGN KEY (post_id) REFERENCES post(id) ON DELETE CASCADE;

ALTER TABLE post_event 
    DROP CONSTRAINT post_event_post_id_fkey,
    ADD CONSTRAINT post_event_post_id_fkey 
        FOREIGN KEY (post_id) REFERENCES post(id) ON DELETE CASCADE;