-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Highest day ceiling admitted for a confirmed full run that UTC day; 0 means no full run was admitted.
ALTER TABLE index_reservations ADD COLUMN IF NOT EXISTS full_ceiling NUMERIC NOT NULL DEFAULT 0;
