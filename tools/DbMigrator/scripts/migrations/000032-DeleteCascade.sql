ALTER TABLE post_event 
    DROP CONSTRAINT post_event_user_platform_integration_id_fkey,
    ADD CONSTRAINT post_event_user_platform_integration_id_fkey 
        FOREIGN KEY (user_platform_integration_id) 
        REFERENCES user_platform_integration(id) 
        ON DELETE CASCADE;