-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Triage signals only: stable numeric codes, no free text. Existing rows stay Unknown (0).
ALTER TABLE interactions ADD COLUMN IF NOT EXISTS category SMALLINT NOT NULL DEFAULT 0;
ALTER TABLE interactions ADD COLUMN IF NOT EXISTS product SMALLINT NOT NULL DEFAULT 0;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'interactions_category_check') THEN
        ALTER TABLE interactions ADD CONSTRAINT interactions_category_check CHECK (category BETWEEN 0 AND 5);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'interactions_product_check') THEN
        ALTER TABLE interactions ADD CONSTRAINT interactions_product_check CHECK (product BETWEEN 0 AND 5);
    END IF;
END
$$;

-- Per-page provenance, published in the index transaction. Unknown (no row) until a successful index run;
-- no historical backfill timestamp is fabricated. Only explicit, bounded values are stored.
CREATE TABLE IF NOT EXISTS documentation_pages (
    page_url TEXT PRIMARY KEY,
    indexed_at TIMESTAMPTZ NOT NULL,
    page_last_modified TIMESTAMPTZ NULL,
    product_version VARCHAR(64) NULL
);
