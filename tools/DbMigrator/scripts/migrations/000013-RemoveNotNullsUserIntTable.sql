ALTER TABLE user_platform_integration 
	ALTER COLUMN expiration DROP NOT NULL,
	ALTER COLUMN token_type DROP NOT NULL;

ALTER TABLE post_media
	RENAME COLUMN image_url TO image_data;

ALTER TABLE platform
	RENAME COLUMN image_url TO image_data;