CREATE EXTENSION IF NOT EXISTS pg_cron;

SELECT cron.schedule(
    'notify-ready-posts',
    '5 seconds', -- OR '1 second',
    $$SELECT CASE 
        WHEN EXISTS(
            SELECT 1 FROM post p
            INNER JOIN post_event pe ON pe.post_id = p.id
            INNER JOIN post_status ps ON pe.post_status_id = ps.id
            WHERE p.scheduled_for <= NOW()
                AND ps.status = 'scheduled'
            LIMIT 1
        ) THEN pg_notify('posts_ready', '')
    END$$
);

CREATE OR REPLACE FUNCTION notify_on_post_change()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.scheduled_for <= NOW() THEN
        PERFORM pg_notify('posts_ready', '');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER post_ready_trigger
AFTER INSERT OR UPDATE OF scheduled_for ON post
FOR EACH ROW
WHEN (NEW.scheduled_for IS NOT NULL)
EXECUTE FUNCTION notify_on_post_change();

--Intensive cron jobs like the prior one creates a lot of logs. Setting up cron.log cleanup
SELECT cron.schedule('cleanup-logs', '0 12 * * *', 
    $$DELETE FROM cron.job_run_details WHERE end_time < NOW() - INTERVAL '2 day'$$
);