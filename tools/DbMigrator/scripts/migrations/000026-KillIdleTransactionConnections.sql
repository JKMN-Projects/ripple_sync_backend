--Removing prior
SELECT cron.unschedule('kill-idle-connections');

SELECT cron.schedule(
    'kill-idle-in-transaction',
    '*/5 * * * *',
    $$
    SELECT pg_terminate_backend(pid) 
    FROM pg_stat_activity 
    WHERE state = 'idle in transaction'
    AND state_change < NOW() - INTERVAL '20 minutes'
    AND datname = current_database()
    AND pid != pg_backend_pid()
    $$
);