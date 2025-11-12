ALTER TABLE post 
    DROP CONSTRAINT post_user_account_id_fkey;

ALTER TABLE user_platform_integration 
    DROP CONSTRAINT user_platform_integration_user_account_id_fkey;

ALTER TABLE user_token 
    DROP CONSTRAINT user_token_user_account_id_fkey;



ALTER TABLE post
    ADD CONSTRAINT post_user_account_id_fkey 
    FOREIGN KEY (user_account_id) 
    REFERENCES user_account(id) 
    ON DELETE CASCADE;

ALTER TABLE user_platform_integration
    ADD CONSTRAINT user_platform_integration_user_account_id_fkey 
    FOREIGN KEY (user_account_id) 
    REFERENCES user_account(id) 
    ON DELETE CASCADE;

ALTER TABLE user_token
    ADD CONSTRAINT user_token_user_account_id_fkey 
    FOREIGN KEY (user_account_id) 
    REFERENCES user_account(id) 
    ON DELETE CASCADE;