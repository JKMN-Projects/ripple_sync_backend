-- Lighter nightly maintenance
SELECT cron.schedule(
    'nightly-maintenance',
    '0 2 * * *',
    $$
    VACUUM ANALYZE post;
    ANALYZE post_event;
    $$
);

-- Weekend deep maintenance
-- Reindex high-activity tables (sunday at 03:00)
SELECT cron.schedule(
    'weekend-deep-maintenance',
    '0 3 * * 0',
    $$
    --CONCURRENTLY wont lock table
    REINDEX TABLE CONCURRENTLY post;
    REINDEX TABLE CONCURRENTLY user_token;
    REINDEX TABLE CONCURRENTLY post_event;
    REINDEX TABLE CONCURRENTLY user_platform_integration;

    --2 version here
    -- With or without FULL. Is cleaner and actually gets disk space back, but fully locks tables.
    --VACUUM FULL post;
    --VACUUM FULL post_event;
    VACUUM ANALYZE post; 
    VACUUM ANALYZE post_event;

    --Analyze updates the planner on current indexes and data, improves query performance
    ANALYZE;
    $$
);

SELECT cron.schedule(
    'kill-idle-connections',
    '*/5 * * * *',  -- Every 5 minutes
    $$
    SELECT pg_terminate_backend(pid) 
        FROM pg_stat_activity 
        WHERE state = 'idle' 
            AND state_change < NOW() - INTERVAL '1 hour'
            AND datname = current_database()
            AND pid != pg_backend_pid() --To avoid killing the cron itself
    $$
);