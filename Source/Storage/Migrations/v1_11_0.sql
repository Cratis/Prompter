-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- The answering kill switch: one row, no actor identity.
CREATE TABLE answering_pause (
    singleton BOOLEAN PRIMARY KEY DEFAULT TRUE CHECK (singleton),
    paused BOOLEAN NOT NULL,
    changed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
