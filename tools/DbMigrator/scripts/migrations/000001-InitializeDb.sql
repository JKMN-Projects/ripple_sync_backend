

CREATE TABLE user_account (
    id 					uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email 				text UNIQUE NOT NULL,
    password_hash   	varchar(100) NOT NULL,
    salt   				varchar(100) NOT NULL,
    created_at 			timestamptz DEFAULT NOW() NOT NULL
);

CREATE TABLE token_type (
	id								int PRIMARY KEY,
	token_name						TEXT UNIQUE NOT NULL
); 

CREATE TABLE user_token (
	id								uuid PRIMARY KEY DEFAULT gen_random_uuid() NOT NULL,
	user_account_id 				uuid REFERENCES user_account(id) NOT NULL,
	token_type_id					int REFERENCES token_type(id) NOT NULL,
	token_value						varchar(100)  NOT NULL,
	created_at						timestamptz DEFAULT NOW() NOT NULL,
	expires_at						timestamptz NOT NULL
);

CREATE TABLE platform (
	id 								int PRIMARY KEY NOT NULL,
	platform_name					TEXT UNIQUE NOT NULL,
	platform_description			TEXT,
	image_url						TEXT
);


CREATE TABLE user_platform_integration (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_account_id uuid REFERENCES user_account(id) NOT NULL,
    platform_id int REFERENCES platform(id) NOT NULL,
    access_token text NOT NULL,
    UNIQUE(user_account_id, platform_id)
);

CREATE TABLE post (
	id								uuid PRIMARY KEY DEFAULT gen_random_uuid() NOT NULL, 
	user_account_id					uuid REFERENCES user_account(id) NOT NULL,
	message_content					TEXT NOT NULL,
	submitted_at					timestamptz NOT NULL,
	updated_at						timestamptz NULL,
	scheduled_for					timestamptz NULL
);

CREATE TABLE post_media (
	id								uuid PRIMARY KEY DEFAULT gen_random_uuid() NOT NULL, 
	post_id 						uuid REFERENCES post(id) NOT NULL,
	image_url						TEXT NOT NULL
);

CREATE TABLE post_status (
	id								int PRIMARY KEY NOT NULL, 
	status							varchar(50) NOT NULL
);

CREATE TABLE post_event (
	post_id 						uuid REFERENCES post(id),
	user_platform_integration_id 	uuid REFERENCES user_platform_integration(id),
	post_status_id					int REFERENCES post_status(id),
	platform_post_identifier		TEXT NOT NULL,
	platform_response				jsonb,
	PRIMARY KEY(post_id, user_platform_integration_id)
);

CREATE INDEX idx_user_account_email ON user_account(email);

CREATE INDEX idx_user_token_user_account_id ON user_token(user_account_id);

CREATE INDEX idx_user_platform_user_account ON user_platform_integration(user_account_id);

CREATE INDEX idx_post_user_account_id ON post(user_account_id);
CREATE INDEX idx_post_scheduled_for ON post(scheduled_for) WHERE scheduled_for IS NOT NULL;

CREATE INDEX idx_post_media_post_id ON post_media(post_id);

CREATE INDEX idx_post_event_user_platform ON post_event(user_platform_integration_id);
CREATE INDEX idx_post_event_platform_identifier ON post_event(platform_post_identifier);

