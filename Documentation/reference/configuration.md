---
title: Configuration
description: Every Prompter setting, its environment variable, and its default - plus the run modes and HTTP endpoints.
---

Configuration binds to the `Cratis:Prompter` section. Environment variables use `__` as the delimiter.

## Settings

| Setting | Environment variable | Default |
|---|---|---|
| Postgres connection string | `Cratis__Prompter__ConnectionString` | localhost, database/user/password `prompter` |
| Docs site to ingest | `Cratis__Prompter__DocsSiteUrl` | `https://cratis.io` |
| Ingestion path exclusions | `Cratis__Prompter__Ingestion__ExcludedPathSegments__0…` | `client-snippets`, `api-reference` |
| Interaction retention (days) | `Cratis__Prompter__RetentionDays` | `90` |
| Discord bot token | `Cratis__Prompter__Discord__Token` | - |
| Ask channel | `Cratis__Prompter__Discord__AskChannelId` | - |
| Help forum channel (auto-reply) | `Cratis__Prompter__Discord__HelpForumChannelId` | - |
| Anthropic API key | `Cratis__Prompter__Anthropic__ApiKey` (or `ANTHROPIC_API_KEY`) | - |
| Answer model | `Cratis__Prompter__Anthropic__Model` | `claude-sonnet-5` |
| Voyage API key | `Cratis__Prompter__Voyage__ApiKey` | - |
| Embedding model | `Cratis__Prompter__Voyage__Model` | `voyage-4` |
| Embedding batch size | `Cratis__Prompter__Voyage__BatchSize` | `128` |
| Embedding dimensions | `Cratis__Prompter__Voyage__Dimensions` | `1024` (must match the database schema) |
| Max passages per answer | `Cratis__Prompter__Answering__MaxPassages` | `8` |
| Refusal threshold | `Cratis__Prompter__Answering__MinScore` | `0.02` |
| Max answer tokens | `Cratis__Prompter__Answering__MaxOutputTokens` | `1024` |

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
| `POST /reindex` | Triggers an ingestion pass; requires the shared-secret header; returns 409 if a run is already in progress |
