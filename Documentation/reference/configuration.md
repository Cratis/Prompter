---
title: Configuration
description: Reference Prompter configuration keys, environment variables and defaults, plus bot and indexing run modes and the HTTP endpoints available in bot mode.
---

Configuration binds to the `Cratis:Prompter` section. Environment variables use `__` as the delimiter.

## Settings

| Setting | Environment variable | Default |
|---|---|---|
| Postgres connection string | `Cratis__Prompter__ConnectionString` | localhost, database/user/password `prompter` |
| Application database role (`migrate` mode only) | `Cratis__Prompter__Migration__ApplicationRole` | unset |
| Application database role password (`migrate` mode only) | `Cratis__Prompter__Migration__ApplicationRolePassword` | unset |
| Docs site to ingest | `Cratis__Prompter__DocsSiteUrl` | `https://cratis.io` |
| Ingestion path exclusions | `Cratis__Prompter__Ingestion__ExcludedPathSegments__0…` | `client-snippets`, `api-reference` |
| Interaction retention (days) | `Cratis__Prompter__RetentionDays` | `90` |
| Discord bot token | `Cratis__Prompter__Discord__Token` | - |
| Ask channel | `Cratis__Prompter__Discord__AskChannelId` | - |
| Help forum channel (auto-reply) | `Cratis__Prompter__Discord__HelpForumChannelId` | - |
| Answer-in-thread channels (off when empty) | `Cratis__Prompter__Discord__AnswerInThreadChannelIds__0`, `__1`, … | - |
| Delayed forum answers (off when empty) | `Cratis__Prompter__Discord__DelayedForums__0__ChannelId` and `__DelayMinutes` (1 to 1440), repeated per forum with `__1__…` | - / `30` |
| Maintainer role for Ask a human, the per-user limit exemption and `/answering` (off when unset) | `Cratis__Prompter__Discord__MaintainerRoleId` | - |
| Daily questions per user (UTC day) | `Cratis__Prompter__Protection__UserAdmissionsPerDay` | `30` (0–100) |
| Minimum seconds between a user's questions | `Cratis__Prompter__Protection__UserAdmissionCooldownSeconds` | `15` (1–86400) |
| Questions per user per window | `Cratis__Prompter__Discord__RateLimit__MaxQuestions` | `10` |
| Refusals that start an escalating pause (0 disables) | `Cratis__Prompter__Protection__RefusalStrikes` | `5` (0–100) |
| Window for counting refusals, seconds | `Cratis__Prompter__Protection__RefusalStrikeWindowSeconds` | `600` (1–86400) |
| First escalated pause, seconds (doubles per repeat) | `Cratis__Prompter__Protection__EscalatedCooldownSeconds` | `300` (1–86400) |
| Longest escalated pause, seconds | `Cratis__Prompter__Protection__MaxEscalatedCooldownSeconds` | `21600` (at most 604800) |
| Only these users may use the bot (off when empty) | `Cratis__Prompter__Protection__AllowedUsers__0`, `__1`, … | - |
| Users refused silently | `Cratis__Prompter__Protection__DeniedUsers__0`, `__1`, … | - |
| Anthropic API key | `Cratis__Prompter__Anthropic__ApiKey` (or `ANTHROPIC_API_KEY`) | - |
| Answer model | `Cratis__Prompter__Anthropic__Model` | `claude-sonnet-5` |
| Voyage API key | `Cratis__Prompter__Voyage__ApiKey` | - |
| Embedding model | `Cratis__Prompter__Voyage__Model` | `voyage-4` |
| Embedding batch size | `Cratis__Prompter__Voyage__BatchSize` | `128` |
| Embedding dimensions | `Cratis__Prompter__Voyage__Dimensions` | `1024` (must match the database schema) |
| Max passages per answer | `Cratis__Prompter__Answering__MaxPassages` | `8` |
| Refusal threshold | `Cratis__Prompter__Answering__MinScore` | `0.02` |
| Max answer tokens | `Cratis__Prompter__Answering__MaxOutputTokens` | `3072` |
| Maintainer channel (weekly metadata-only gap digest) | `Cratis__Prompter__Discord__MaintainerChannelId` | - (digest off) |
| Weekly digest UTC day | `Cratis__Prompter__Discord__MaintainerDigestDay` | `Monday` |
| Weekly digest UTC hour (clamped to 0–23) | `Cratis__Prompter__Discord__MaintainerDigestHourUtc` | `8` |

API keys are never committed - use environment variables or a git-ignored
`Source/appsettings.Development.json`.

## Run modes

| Command | Does |
|---|---|
| `dotnet run` | Runs the Discord bot |
| `dotnet run -- index` | One ingestion pass; prints a summary (pages, embedded, unchanged, removed, duration) |
| `dotnet run -- ask "<question>"` | Answers from the terminal; add `--verbose` to print retrieved passages; exits non-zero on refusal |

## HTTP endpoints (bot mode)

| Endpoint | Does |
|---|---|
| `GET /healthz` | Liveness/readiness - database and gateway connectivity |
| `POST /reindex` | In-cluster/local ingestion endpoint; requires the shared-secret header; returns 409 if a run is already in progress. Production has no public route; use **Reindex - Production** on `main` |
