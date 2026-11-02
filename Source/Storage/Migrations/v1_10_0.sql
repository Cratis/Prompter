-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

CREATE TABLE github_issue_deliveries (
    repository TEXT NOT NULL,
    issue_number INTEGER NOT NULL,
    claimed_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (repository, issue_number)
);
