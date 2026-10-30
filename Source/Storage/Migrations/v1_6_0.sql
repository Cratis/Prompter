-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Additive attempt accounting. Funding remains in the existing shared UTC-day ledger.
CREATE TABLE IF NOT EXISTS prompter_provider_attempts (
    operation_id uuid NOT NULL,
    provider text NOT NULL CHECK (provider IN ('anthropic', 'voyage')),
    account text NOT NULL CHECK (account IN ('primary', 'secondary')),
    task text NOT NULL CHECK (task IN ('answer', 'issue-draft', 'query', 'index')),
    attempt integer NOT NULL CHECK (attempt BETWEEN 1 AND 11),
    model text NOT NULL CHECK (length(model) BETWEEN 1 AND 128),
    effort text NOT NULL CHECK (effort IN ('', 'low', 'medium', 'high')),
    reservation uuid NOT NULL REFERENCES prompter_spend_reservations(id),
    day date NOT NULL REFERENCES prompter_spend_days(day),
    prepaid boolean NOT NULL,
    upper_usd numeric NOT NULL CHECK (upper_usd > 0),
    completed boolean NOT NULL DEFAULT false,
    known_usd numeric CHECK (known_usd >= 0 AND known_usd <= upper_usd),
    PRIMARY KEY (operation_id, provider, attempt)
);

CREATE OR REPLACE FUNCTION reserve_prompter_attempt(
    op uuid, prov text, acct text, work text, number integer, model_id text, reasoning text,
    parent uuid, upper_cost numeric, global_cap numeric, chat_cap numeric, embedding_cap numeric
) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE
    funding uuid := parent;
    reserved prompter_spend_reservations%ROWTYPE;
    allocated numeric;
BEGIN
    IF upper_cost <= 0 OR upper_cost > global_cap OR prov NOT IN ('anthropic', 'voyage')
        OR (prov = 'anthropic' AND number > 2) OR (prov = 'voyage' AND acct <> 'primary') THEN
        RETURN false;
    END IF;
    -- Serialize duplicate attempts even with independent connection pools/accounts.
    PERFORM pg_advisory_xact_lock(hashtextextended(op::text || prov, 0));
    IF EXISTS (SELECT 1 FROM prompter_provider_attempts WHERE operation_id = op AND provider = prov AND attempt = number) THEN
        RETURN false;
    END IF;
    IF parent IS NOT NULL THEN
        SELECT * INTO reserved FROM prompter_spend_reservations WHERE id = parent FOR UPDATE;
        IF NOT FOUND OR reserved.settled OR (prov = 'anthropic' AND number <> 1) THEN
            RETURN false;
        END IF;
        SELECT COALESCE(sum(upper_usd), 0) INTO allocated FROM prompter_provider_attempts
            WHERE reservation = parent AND provider = prov;
        IF allocated + upper_cost > CASE WHEN prov = 'anthropic' THEN reserved.anthropic ELSE reserved.voyage END THEN
            RETURN false;
        END IF;
    ELSE
        funding := reserve_prompter_spend(gen_random_uuid(), CASE WHEN prov = 'anthropic' THEN upper_cost ELSE 0 END,
            CASE WHEN prov = 'voyage' THEN upper_cost ELSE 0 END, global_cap, chat_cap, embedding_cap);
        IF funding IS NULL THEN
            RETURN false;
        END IF;
        SELECT * INTO reserved FROM prompter_spend_reservations WHERE id = funding;
    END IF;
    INSERT INTO prompter_provider_attempts(operation_id, provider, account, task, attempt, model, effort, reservation, day, prepaid, upper_usd)
        VALUES (op, prov, acct, work, number, model_id, reasoning, funding, reserved.day, parent IS NOT NULL, upper_cost);
    RETURN true;
END;
$$;

CREATE OR REPLACE FUNCTION complete_prompter_attempt(
    op uuid, prov text, acct text, work text, number integer, model_id text, reasoning text, measured numeric
) RETURNS boolean LANGUAGE plpgsql AS $$
DECLARE
    entry prompter_provider_attempts%ROWTYPE;
BEGIN
    SELECT * INTO entry FROM prompter_provider_attempts
        WHERE operation_id = op AND provider = prov AND attempt = number FOR UPDATE;
    IF NOT FOUND OR entry.account <> acct OR entry.task <> work OR entry.model <> model_id OR entry.effort <> reasoning THEN
        RETURN false;
    END IF;
    IF entry.completed THEN
        RETURN false;
    END IF;
    IF measured < 0 OR measured > entry.upper_usd THEN
        measured := NULL;
    END IF;
    IF NOT entry.prepaid THEN
        PERFORM reconcile_prompter_spend(entry.reservation,
            CASE WHEN prov = 'anthropic' THEN COALESCE(measured, entry.upper_usd) ELSE 0 END,
            CASE WHEN prov = 'voyage' THEN COALESCE(measured, entry.upper_usd) ELSE 0 END);
    END IF;
    UPDATE prompter_provider_attempts SET completed = true, known_usd = measured
        WHERE operation_id = op AND provider = prov AND attempt = number;
    RETURN true;
END;
$$;
