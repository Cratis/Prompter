-- Data minimization (D-13, amending D-8): the interaction log keeps no personal data. Drop the message
-- content (question, answer), the user pseudonym (user_hash), and the answer message id (which the feedback
-- flow never used) - leaving only anonymous operational signal: source, cited pages, confidence, refusal,
-- and feedback. With no personal data stored, the log falls outside GDPR retention/DSAR obligations entirely.
ALTER TABLE interactions DROP COLUMN IF EXISTS question;
ALTER TABLE interactions DROP COLUMN IF EXISTS answer;
ALTER TABLE interactions DROP COLUMN IF EXISTS user_hash;
ALTER TABLE interactions DROP COLUMN IF EXISTS answer_message_id;
