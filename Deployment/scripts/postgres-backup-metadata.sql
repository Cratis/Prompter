-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.
BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY;
SELECT :'snapshot' <> '' AS use_snapshot \gset
\if :use_snapshot
SET TRANSACTION SNAPSHOT :'snapshot';
\endif
DO $$ BEGIN
    IF current_setting('server_version_num')::int / 10000 <> 17 THEN
        RAISE EXCEPTION 'Postgres 17 required';
    END IF;
    IF NOT EXISTS (SELECT FROM pg_extension WHERE extname = 'vector') THEN
        RAISE EXCEPTION 'pgvector required';
    END IF;
    IF NOT EXISTS (SELECT FROM chunks) OR EXISTS (SELECT FROM chunks WHERE vector_dims(embedding) <> 1024) THEN
        RAISE EXCEPTION 'Expected a nonempty 1024-dimensional corpus';
    END IF;
    IF NOT EXISTS (SELECT FROM schema_migrations WHERE version = '1.2.0') THEN
        RAISE EXCEPTION 'Privacy migration required before backup or restore';
    END IF;
END $$;
SELECT json_build_object(
    'format', 1,
    'postgresMajor', current_setting('server_version_num')::int / 10000,
    'vectorVersion', (SELECT extversion FROM pg_extension WHERE extname = 'vector'),
    'migrations', (SELECT json_agg(version ORDER BY version) FROM schema_migrations),
    'columns', (SELECT json_agg(row(table_name, column_name, data_type, udt_name, is_nullable) ORDER BY table_name, ordinal_position)
        FROM information_schema.columns WHERE table_schema = 'public'),
    'indexes', (SELECT json_agg(indexdef ORDER BY tablename, indexname) FROM pg_indexes WHERE schemaname = 'public'),
    'chunks', (SELECT count(*) FROM chunks),
    'interactions', (SELECT count(*) FROM interactions),
    'feedback', (SELECT json_agg(row(feedback, total) ORDER BY feedback) FROM
        (SELECT feedback, count(*) AS total FROM interactions GROUP BY feedback) counts),
    'dimensions', (SELECT json_build_array(min(vector_dims(embedding)), max(vector_dims(embedding))) FROM chunks),
    'retrieval', (SELECT count(*) FROM (SELECT id FROM chunks ORDER BY embedding <=> (SELECT embedding FROM chunks ORDER BY id LIMIT 1) LIMIT 1) nearest),
    'textSearch', (SELECT count(*) FROM chunks WHERE tsv @@ plainto_tsquery('english', 'Cratis'))
);
ROLLBACK;
