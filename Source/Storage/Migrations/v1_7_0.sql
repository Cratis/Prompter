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
