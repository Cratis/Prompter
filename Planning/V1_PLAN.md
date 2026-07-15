# V1 Plan — the light roadmap

**Goal:** Prompter answers Cratis questions in the community Discord, grounded in the published docs at
<https://cratis.io>, with citations on every answer and a measured (not assumed) answer quality. This is the
one-page "what are we doing and in what order" view — task detail lives in
[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md), the work list in [`BACKLOG.md`](BACKLOG.md), the Discord
behavior contract in [`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md), the ops runbook in
[`DEPLOYMENT.md`](DEPLOYMENT.md), decisions in [`DECISIONS.md`](DECISIONS.md), background in
[`RESEARCH.md`](RESEARCH.md).

> Status snapshot (2026-07-15): repo scaffolded — solution builds, spec suite green, docker-compose brings up
> Postgres + pgvector, feature folders and contracts in place with skeleton implementations. Milestones M1–M5
> are unstarted.

## The shape of the system

```
cratis.io (.md page mirrors + llms.txt)          Discord (mention / #ask / help forum)
        │                                                 │
        ▼                                                 ▼
   Ingestion ──chunks+embeddings──▶ Postgres+pgvector ◀── Retrieval (hybrid BM25+vector, RRF)
   (heading chunking,                                     │
    hash-keyed incremental)                               ▼
        ▲                                            Answering (Claude via IChatClient,
        │                                             citations required, refusal policy)
   re-index webhook ◀── Documentation repo `build-docs` repository_dispatch
```

One deployable (`Source/Prompter`), one database, one box. Run cost target: ≤ €15/month all-in
([`RESEARCH.md`](RESEARCH.md) §Cost).

## Milestones — build in this order

Each milestone is shippable and verified before the next starts. Items carry P-numbers in
[`BACKLOG.md`](BACKLOG.md).

| # | Milestone | Outcome that proves it |
|---|---|---|
| M0 | **Scaffold** ✅ | `dotnet build` zero warnings, specs green, `docker compose up` gives a pgvector-enabled Postgres |
| M1 | **Ingestion** | `prompter index` ingests cratis.io into chunks+embeddings; second run re-embeds only changed chunks; `client-snippets` excluded |
| M2 | **Retrieval + Answering** | `prompter ask "<question>"` returns a grounded, cited answer in the terminal; low-score questions get an honest refusal |
| M3 | **Discord** | @Prompter mention and `/ask` work in the Cratis server; auto-reply in the designated help forum channel; per-user rate limiting |
| M4 | **Evaluation** | Golden Q&A set (≥40 questions) scored in CI; groundedness/citation regression gate; prompt iterated against real questions |
| M5 | **Operations** | Deployed (Hetzner, Docker Compose), `build-docs` webhook re-indexes, logs/alerts, privacy notice posted in Discord, retention job active |

**The eval milestone (M4) is not optional.** The recurring failure mode of self-built docs bots is tuning
neglect — Astro archived theirs for exactly this ([`RESEARCH.md`](RESEARCH.md) §Market). M4 is what makes
quality a measured property instead of a hope, and it gates every later prompt/retrieval change.

## Definition of done (v1)

- Answers cite source pages (links) or explicitly refuse; no uncited claims.
- Eval harness ≥ agreed thresholds on the golden set, wired into CI.
- Re-index happens automatically within minutes of docs merging to `main`.
- Interaction data handling matches [`DECISIONS.md`](DECISIONS.md) D-8 (GDPR: hashed user IDs, retention,
  privacy notice).
- `dotnet build` zero warnings, specs green, deploy is one documented command.

## Post-v1 candidates (parked, not promised)

Confidence-gated chime-in on opted-in channels · docs-MCP server exposing the same retrieval · Answer Overflow
alongside · Chronicle dogfooding for the interaction log (D-6) · weekly "unanswered questions" digest feeding
docs coverage gaps. See the parking lot in [`BACKLOG.md`](BACKLOG.md).
