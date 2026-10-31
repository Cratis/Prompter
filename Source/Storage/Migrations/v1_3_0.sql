-- Copyright (c) Cratis. All rights reserved.
-- Licensed under the MIT license. See LICENSE file in the project root for full license information.

-- Additive: old binaries ignore these aggregate-only tables. Never record Discord or prompt data.
CREATE TABLE IF NOT EXISTS prompter_spend_days (
    day date PRIMARY KEY,
    anthropic numeric NOT NULL DEFAULT 0 CHECK (anthropic >= 0),
    voyage numeric NOT NULL DEFAULT 0 CHECK (voyage >= 0)
);
CREATE TABLE IF NOT EXISTS prompter_spend_reservations (
    id uuid PRIMARY KEY,
    day date NOT NULL REFERENCES prompter_spend_days(day),
    anthropic numeric NOT NULL CHECK (anthropic >= 0),
    voyage numeric NOT NULL CHECK (voyage >= 0),
    settled boolean NOT NULL DEFAULT false
);

CREATE OR REPLACE FUNCTION reserve_prompter_spend(
    reservation uuid, chat numeric, embedding numeric, global_cap numeric, chat_cap numeric, embedding_cap numeric
) RETURNS uuid LANGUAGE plpgsql AS $$
DECLARE
    utc_day date := (clock_timestamp() AT TIME ZONE 'UTC')::date;
    current_day prompter_spend_days%ROWTYPE;
BEGIN
    IF chat < 0 OR embedding < 0 OR chat + embedding <= 0 THEN
        RETURN NULL;
    END IF;
    INSERT INTO prompter_spend_days(day) VALUES (utc_day) ON CONFLICT DO NOTHING;
    SELECT * INTO current_day FROM prompter_spend_days WHERE day = utc_day FOR UPDATE;
    IF current_day.anthropic + current_day.voyage + chat + embedding > global_cap
        OR current_day.anthropic + chat > chat_cap
        OR current_day.voyage + embedding > embedding_cap THEN
        RETURN NULL;
    END IF;
    INSERT INTO prompter_spend_reservations(id, day, anthropic, voyage)
        VALUES (reservation, utc_day, chat, embedding);
    UPDATE prompter_spend_days SET anthropic = anthropic + chat, voyage = voyage + embedding WHERE day = utc_day;
    RETURN reservation;
END;
$$;

CREATE OR REPLACE FUNCTION reconcile_prompter_spend(reservation uuid, chat numeric, embedding numeric)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE
    reserved prompter_spend_reservations%ROWTYPE;
BEGIN
    SELECT * INTO reserved FROM prompter_spend_reservations WHERE id = reservation FOR UPDATE;
    IF NOT FOUND OR reserved.settled THEN
        RETURN;
    END IF;
    IF chat < 0 OR embedding < 0 THEN
        RAISE EXCEPTION 'Invalid spend reconciliation';
    END IF;
    UPDATE prompter_spend_days SET anthropic = anthropic + chat - reserved.anthropic,
        voyage = voyage + embedding - reserved.voyage WHERE day = reserved.day;
    UPDATE prompter_spend_reservations SET anthropic = chat, voyage = embedding, settled = true WHERE id = reservation;
END;
$$;
