-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Existing product values stay unchanged; ecosystem products are appended.
ALTER TABLE interactions DROP CONSTRAINT IF EXISTS interactions_product_check;
ALTER TABLE interactions ADD CONSTRAINT interactions_product_check CHECK (product BETWEEN 0 AND 12);
