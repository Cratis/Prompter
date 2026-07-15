# Prompter

[![Discord](https://img.shields.io/discord/1182595891576717413?label=Discord&logo=discord&logoColor=white)](https://discord.gg/kt4AMpV8WV)
[![Docker](https://img.shields.io/docker/v/cratis/prompter?label=Prompter&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/prompter)
[![Build](https://github.com/Cratis/Prompter/actions/workflows/build.yml/badge.svg)](https://github.com/Cratis/Prompter/actions/workflows/build.yml)
[![Publish](https://github.com/Cratis/Prompter/actions/workflows/publish.yml/badge.svg)](https://github.com/Cratis/Prompter/actions/workflows/publish.yml)

The Cratis community's documentation assistant on Discord. Like a theater prompter, it sits just offstage with
the script — the published docs at [cratis.io](https://cratis.io) — and feeds you the line you're missing:
mention **@Prompter** or use **/ask**, and it answers grounded in the documentation, with citations, or refuses
honestly when the docs don't cover it.

Built with C#/.NET 10, [NetCord](https://netcord.dev), [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/),
Claude (Anthropic), Voyage AI embeddings, and Postgres + pgvector with hybrid (BM25 + vector) retrieval.

## Start here

- [`Planning/SESSION_HANDOVER.md`](Planning/SESSION_HANDOVER.md) — current state and next actions (start here to continue work).
- [`Planning/V1_PLAN.md`](Planning/V1_PLAN.md) — the roadmap: milestones M0–M5 and the definition of done.
- [`Planning/IMPLEMENTATION_PLAN.md`](Planning/IMPLEMENTATION_PLAN.md) — the detailed plan to feature-complete v1.
- [`Planning/DISCORD_INTEGRATION.md`](Planning/DISCORD_INTEGRATION.md) — the Discord behavior contract and app-setup runbook.
- [`Planning/DECISIONS.md`](Planning/DECISIONS.md) — durable decisions (name, buy-vs-build, stack, GDPR).
- [`Documentation/architecture.md`](Documentation/architecture.md) — how ingestion, retrieval, and answering fit together.

## Quick start

Bring up Postgres (with pgvector):

```bash
docker compose up -d
```

Index the documentation, then ask a question from the terminal:

```bash
cd Source
dotnet run -- index
dotnet run -- ask "How do I append an event in Chronicle?"
```

Run as the Discord bot:

```bash
dotnet run
```

## Configuration

Configuration binds to the `Cratis:Prompter` section (environment variables use `__` as delimiter):

| Setting | Environment variable | Default |
|---|---|---|
| Postgres connection string | `Cratis__Prompter__ConnectionString` | localhost, db/user/pass `prompter` |
| Docs site to ingest | `Cratis__Prompter__DocsSiteUrl` | `https://cratis.io` |
| Discord bot token | `Cratis__Prompter__Discord__Token` | — |
| Help forum channel (auto-reply) | `Cratis__Prompter__Discord__HelpForumChannelId` | — |
| Anthropic API key | `Cratis__Prompter__Anthropic__ApiKey` (or `ANTHROPIC_API_KEY`) | — |
| Answer model | `Cratis__Prompter__Anthropic__Model` | `claude-sonnet-5` |
| Voyage API key | `Cratis__Prompter__Voyage__ApiKey` | — |
| Interaction retention (days) | `Cratis__Prompter__RetentionDays` | `90` |

## Quality gates

```bash
dotnet build --configuration Release   # zero warnings, zero errors
dotnet test --configuration Release    # all specs pass
```

Answer quality is a measured property: the golden-question eval harness (milestone M4) gates prompt and
retrieval changes the same way specs gate code changes.
