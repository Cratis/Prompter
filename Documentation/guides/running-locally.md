---
title: Run Prompter locally
description: Bring the bot up on your own machine, index the documentation, and ask questions from the terminal or the Cratis Discord server.
---

Run the full pipeline - ingestion, retrieval, answering, and optionally the Discord bot - on your own machine.

## Prerequisites

- .NET SDK 10.0.301 or later
- Docker (for Postgres with pgvector)
- A [Voyage AI](https://www.voyageai.com) API key (free tier, required for indexing)
- An [Anthropic](https://www.anthropic.com) API key (required for answering)
- Optional: a Discord bot token for the Cratis server (only for running the bot itself)

Before starting an answer-enabled host, explicitly set the scalar
`Cratis__Prompter__Protection__ConfiguredAllowedGuilds=1182595891576717413`. This is required even when
using only the terminal: the bot and the ask, index, and eval commands share the startup check. The
migrate command is separate. Only the Cratis guild is allowed, with no local or Development exemption.
If answering is disabled, the guild list may be empty, but it cannot name a different guild, and
admission remains disabled.

## Bring up the database

From the repository root:

```bash
docker compose up -d
```

This starts Postgres with the pgvector extension on `localhost:5432`. The schema is created automatically on
first run.

## Configure your keys

Set the keys as environment variables (or use a git-ignored `Source/appsettings.Development.json` - never
commit keys):

```bash
export Cratis__Prompter__Voyage__ApiKey=<your-voyage-key>
export Cratis__Prompter__Anthropic__ApiKey=<your-anthropic-key>
```

## Index the documentation

```bash
cd Source
dotnet run -- index
```

This walks the cratis.io sitemap, fetches every page's markdown mirror, chunks and embeds the content, and
prints a summary line. A second run reports everything unchanged - only modified content is ever re-embedded.

## Ask from the terminal

```bash
dotnet run -- ask "How do I append an event in Chronicle?"
dotnet run -- ask "How do I append an event in Chronicle?" --verbose
```

The `--verbose` flag prints the retrieved passages with their scores before the answer - useful for judging
retrieval quality. The command exits non-zero when Prompter refuses, so it works in scripts.

## Run the Discord bot

Follow [Set up the Discord app](discord-setup.md) for the application and Cratis guild configuration.
The same guild restriction applies to this locally hosted bot. Then:

```bash
export Cratis__Prompter__Discord__Token=<your-test-bot-token>
dotnet run
```

The bot connects outward to Discord - no ports, tunnels, or public IP needed - and answers only in the
allowed Cratis guild. Local hosting does not enable a separate private test guild. See
[Configuration](../reference/configuration.md) for every setting.
