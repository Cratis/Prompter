-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Baseline for the shipped voyage-4/1024 defaults; verify the legacy corpus matches
-- before upgrade. A changed query model requires an approved compatible full replacement.
CREATE TABLE index_profile (
    singleton BOOLEAN PRIMARY KEY DEFAULT TRUE CHECK (singleton),
    profile TEXT NOT NULL
);
INSERT INTO index_profile VALUES (TRUE, 'voyage-4/1024/markdown-v1');

CREATE TABLE index_reservations (
    day DATE PRIMARY KEY,
    tokens BIGINT NOT NULL DEFAULT 0,
    dollars NUMERIC NOT NULL DEFAULT 0,
    scheduled BOOLEAN NOT NULL DEFAULT FALSE
);
