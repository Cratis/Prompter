-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Aggregate only: no URLs, user/channel identities, request content or error details.
CREATE TABLE prompter_index_health (
    singleton boolean PRIMARY KEY DEFAULT true CHECK (singleton),
    attempts bigint NOT NULL DEFAULT 0,
    successes bigint NOT NULL DEFAULT 0,
    failures bigint NOT NULL DEFAULT 0,
    cancellations bigint NOT NULL DEFAULT 0,
    last_attempt timestamptz,
    last_success timestamptz,
    last_failure timestamptz,
    duration_seconds double precision NOT NULL DEFAULT 0,
    documents bigint NOT NULL DEFAULT 0,
    chunks bigint NOT NULL DEFAULT 0
);
INSERT INTO prompter_index_health(singleton) VALUES (true);
