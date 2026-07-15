-- Feedback capture (P-16): record which answer message a Q&A produced and the verdict a user
-- gives it through the thumbs-up/down buttons. Both columns are nullable - feedback is optional,
-- and older rows predate the answer message id.
ALTER TABLE interactions ADD COLUMN IF NOT EXISTS answer_message_id TEXT;
ALTER TABLE interactions ADD COLUMN IF NOT EXISTS feedback TEXT;
