
INSERT INTO token_type (id, token_name) VALUES
	(1, 'login'),
	(2, 'refresh');
--	(3, 'password_reset'),
--	(4, 'email_verification');

INSERT INTO post_status (id, status) VALUES
	(1, 'scheduled'),
	(2, 'processing'),
	(3, 'posted'),
	(4, 'failed');