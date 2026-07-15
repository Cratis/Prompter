CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS chunks (
    id TEXT PRIMARY KEY,
    page_url TEXT NOT NULL,
    title TEXT NOT NULL,
    heading_path TEXT NOT NULL,
    content TEXT NOT NULL,
    content_hash TEXT NOT NULL,
    embedding vector(1024) NOT NULL,
    tsv tsvector GENERATED ALWAYS AS (to_tsvector('english', content)) STORED
);

CREATE INDEX IF NOT EXISTS chunks_tsv_idx ON chunks USING GIN (tsv);

CREATE TABLE IF NOT EXISTS interactions (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    user_hash TEXT NOT NULL,
    source TEXT NOT NULL,
    question TEXT NOT NULL,
    answer TEXT NOT NULL,
    cited_pages TEXT[] NOT NULL,
    confidence DOUBLE PRECISION NOT NULL,
    was_refusal BOOLEAN NOT NULL
);

CREATE INDEX IF NOT EXISTS interactions_occurred_at_idx ON interactions (occurred_at);
