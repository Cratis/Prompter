---
title: Run Prompter locally
description: Bring the bot up on your own machine, index the documentation, and ask questions from the terminal or a test Discord server.
---

Run the full pipeline - ingestion, retrieval, answering, and optionally the Discord bot - on your own machine.

## Prerequisites

- .NET SDK 10.0.301 or later
- Docker (for Postgres with pgvector)
- A [Voyage AI](https://www.voyageai.com) API key (free tier, required for indexing)
- An [Anthropic](https://www.anthropic.com) API key (required for answering)
- Optional: a Discord bot token for a private test server (only for running the bot itself)

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

Create a bot application on a private test server first - the setup steps live in the repository's
[Discord integration runbook](https://github.com/Cratis/Prompter/blob/main/Planning/DISCORD_INTEGRATION.md).
Then:

```bash
export Cratis__Prompter__Discord__Token=<your-test-bot-token>
dotnet run
```

The bot connects outward to Discord - no ports, tunnels, or public IP needed - and answers on your test
server exactly as it would in production. See [Configuration](../reference/configuration.md) for every
setting.
