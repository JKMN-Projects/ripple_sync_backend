
ALTER TABLE user_platform_integration
    ADD COLUMN refresh_token text,
    ADD COLUMN expiration timestamptz NOT NULL,
    ADD COLUMN token_type text NOT NULL,
    ADD COLUMN scope text;