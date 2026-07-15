# Backlog

The single consolidated work list. Milestone framing lives in [`V1_PLAN.md`](V1_PLAN.md); **the detailed
how/where/done-when for every P-item lives in [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md)**; decisions
in [`DECISIONS.md`](DECISIONS.md). Every item gets a P-number; open questions get Q-numbers; things we chose
not to promise live in the parking lot.

## M1 — Ingestion

- **P-01** ~~Validate the page inventory source against the real site~~ **Verified 2026-07-15**: `llms.txt`
  on cratis.io is only a pointer index (to `llms-small.txt`/`llms-full.txt`), so ingestion walks
  `sitemap-0.xml` (870 pages) and fetches each page's `.md` mirror (`<path>.md`, root → `index.md`) —
  implemented and spec-covered. ~~Remaining residue: strip remaining MDX component tags.~~ **MDX residue
  done 2026-07-15**: `MarkdownChunker.StripMdxComponents` strips imports, JSX `{/* */}` comments and
  block-level component tags (paired, self-closing, multi-line) outside code fences, keeping prose children;
  specced against real `index.md`/`arc.md` mirror fixtures. Open: evaluate `llms-full.txt` as a cheaper
  single-fetch alternative; inline (mid-line) components are not stripped (none occur in the real mirrors).
- **P-02** ~~Batch embedding calls~~ **Done 2026-07-15** (code): `Indexer` batches changed chunks into
  requests of `Voyage:BatchSize` (default 128; verified against Voyage docs — voyage-4 allows 1,000 inputs /
  320K tokens, so 128 is safely under both), with a character guard, and `ResilientEmbeddingGenerator` retries
  429/5xx with exponential backoff. Batching + retry are spec-covered; **the full-corpus live run (done-when)
  is pending a Voyage API key.**
- **P-03** ~~Make the ingestion exclusion list configurable~~ **Done 2026-07-15**: `IngestionOptions.
  ExcludedPathSegments` (defaults `client-snippets`, `api-reference`) consumed by `DocsSite`; `ParsePageUrls`
  takes the list as a parameter; custom + default exclusions spec-covered.
- **P-04** ~~Decide the schema-migration story~~ **Done 2026-07-15**: versioned SQL migrations (Ada-style).
  `Storage/Migrations/v1_0_0.sql` (the former `Schema.sql`) + a `schema_migrations` tracking table; `EnsureSchema`
  discovers embedded migrations, orders by parsed `MigrationVersion`, and applies only pending ones, each in a
  `BEGIN…COMMIT` with its version-record insert (no partial-record risk). Pure `MigrationVersion`/`MigrationPlan`
  logic spec-covered; live-verified against Postgres (fresh build + idempotent re-run + a `v1_1_0` applied on top).
  Unblocks P-16 (add `answer_message_id` as `v1_1_0.sql`, zero code changes).
- **P-05** `prompter index` run in CI on a schedule as a fallback for the webhook (M5).

## M2 — Retrieval + Answering

- **P-06** Tune hybrid search: candidate pool size, RRF constant, and whether headings should be part of the
  `tsvector`. Measure against the golden set (P-17), don't guess.
- **P-07** Calibrate the refusal threshold (`Answering:MinScore`) on real questions — the default is a guess.
- **P-08** Consider a query-rewrite step (cheap model) for conversational questions before retrieval.
- **P-09** Enable Anthropic prompt caching for the system prompt + excerpt scaffold once traffic justifies it.
- **P-10** ~~`prompter ask` should print confidence and passage provenance with a `--verbose` flag~~
  **Done 2026-07-15** (code): `ask --verbose` lists the retrieved passages (score/page/heading, best first)
  before the answer, and `ask` exits non-zero on a refusal (for scripts/CI probes). The parse/render/exit
  logic is pure and spec-covered (`AskArguments`, `AskOutput`); `Answer` now carries the `Passages` it was
  grounded in. **The live `--verbose` run against the real corpus is pending Voyage + Anthropic keys.**

## M3 — Discord

- **P-11** ~~Deferred responses for `/ask`~~ **Done 2026-07-15** (code): `/ask` now sends
  `InteractionCallback.DeferredMessage()` via `SendResponseAsync` (the native "thinking…"), computes the answer,
  then delivers it with `SendFollowupMessageAsync`; return type changed to `Task` so NetCord doesn't
  double-respond. NetCord beta.11 API confirmed against the shipped assembly + netcord.dev. **Live "thinking…
  → answer" runtime check is the M3.1 done-when (needs a test server + keys).**
- **P-12** Verify `GatewayClient.Id` correctly identifies the bot for mention detection (scaffold assumption),
  and handle role-mentions (`<@&…>`) and the nickname mention form (`<@!…>`).
- **P-13** ~~Forum auto-reply~~ **Done 2026-07-15** (code): `HelpForum` implements NetCord's
  `IGuildThreadCreateGatewayHandler` — on a newly-created thread whose `ParentId` matches
  `Discord:HelpForumChannelId`, it reads the starter message, answers as the first reply, then posts the standing
  "A human will follow up…" line (two sends so the 2000-char guarantee holds). Pure `ShouldAnswer` guard
  spec-covered (5 facts); auto-registered by assembly scan. **Live forum-post check is the M3.4 done-when.**
- **P-14** ~~Per-user rate limiting (e.g. 5 questions / 10 min)~~ **Done 2026-07-15** (logic): `RateLimiter`
  is a pure per-user token bucket (`TryConsume(userHash, now)`), config `Discord:RateLimit` (`MaxQuestions`
  5 / `WindowMinutes` 10), spec-covered (within-limit, exceed, window refill, partial refill, per-user
  isolation). **Wiring into the gateway handler + the friendly refusal message is M3 integration** (needs the
  NetCord-verified message path).
- **P-15** ~~Split answers over 2000 chars instead of truncating~~ **Done 2026-07-15** (code): pure
  `DiscordAnswers.Split(Answer) : IReadOnlyList<string>` packs paragraphs greedily into ≤2000-char chunks (max 3,
  sources on the last), hard-splits oversized paragraphs, falls back to `Format` for short answers; `Mentions`
  sends each chunk in order. 15 facts. Follow-up refinement queued from `DISCORD_BEST_PRACTICES.md`: avoid
  splitting inside a fenced code block.
- **P-16** 👍/👎 feedback reactions on bot answers, recorded onto the interaction row.
- **P-17a** Register the Discord application, enable the Message Content intent, generate the invite URL with
  minimal permissions (Send Messages, Create Public Threads, Embed Links) — team action.

## M4 — Evaluation

- **P-17** ~~Author a golden Q&A set (≥40 questions) spanning Chronicle, Arc, Fundamentals, Components, cli~~
  **Done 2026-07-15**: `Eval/golden-questions.yaml` (+ `Eval/README.md`) — **57 in-scope** questions across all
  five products + **12 out-of-scope refusals** (incl. adversarial near-misses like EventStoreDB/Marten, Kafka
  consumer groups). ~39 pages content-verified against the real `.md` mirrors; the rest sitemap-confirmed.
  Schema (id/product/type/question/expected/expected_pages/rationale) documents exactly how P-18 will score it.
- **P-18** Eval harness scoring groundedness, citation correctness, and refusal behavior — crib
  `dotnet/eShopSupport`'s `AnswerScoringEvaluator` and `Microsoft.Extensions.AI.Evaluation`.
- **P-19** Wire the eval as a CI gate for prompt/retrieval changes (score regression fails the build).

## M5 — Operations

- **P-20** Re-index webhook: small HTTP endpoint (shared-secret protected) triggered by the Documentation
  repo's `build-docs` `repository_dispatch` chain, calling `IIndexer.Run()`.
- **P-21** Deploy: join the existing UpCloud UKS cluster per D-11 — Prompter workload + in-cluster
  Postgres/pgvector + ingress route + `deploy-production.yml` modeled on Studio's (`Studio/Deployment/` is
  the reference). Resolve Q-5 (Pulumi code in Studio's stack vs. this repo) first.
- **P-22** Retention purge job — schedule `IInteractionLog.PurgeExpired()` daily (RetentionDays default 90).
- **P-23** Privacy notice: pinned Discord message + docs page naming the bot, what it stores (hashed user IDs,
  questions/answers, 90 days), and the LLM subprocessor. See D-8.
- **P-24** Register Prompter in the `Documentation` repo: `PRODUCTS[]` entry in `web/scripts/sync-content.mjs`
  + sibling-clone list, so `Documentation/` here appears on cratis.io.
- **P-25** Run the `sync-copilot-instructions` workflow to pull the shared `.ai/` + `.claude/` + `.github`
  config from the AI repo (do not hand-copy rules).
- **P-26** ~~Repo settings~~ **Mostly done 2026-07-15**: `Cratis/Prompter` created (public, D-12) and pushed;
  secrets are **org-level** and confirmed reaching this repo (live `documentation.yml` dispatch succeeded;
  Chronicle.Mcp publishes with zero repo secrets). Residue: the Docker Hub `cratis/prompter` repository if
  the first publish doesn't auto-create it.

## Content roadmap (design owned by [`CONTENT_AND_FRESHNESS.md`](CONTENT_AND_FRESHNESS.md))

Phase 1 (docs site) is the v1 corpus and is covered by M1/M5 above. These extend the content base after v1:

- **P-27** Phase 2: **release notes** source — ingest GitHub Releases of Chronicle, Arc, Fundamentals,
  Components, cli as tagged chunks with "release note" citation attribution; refresh on `release` webhook or
  nightly. The freshest signal we have between docs updates.
- **P-28** Phase 2: **glossary + AI-rules grounding** — fold `AI/.ai/rules/glossary.md` (and the writing
  conventions' terminology) into the system prompt so answers speak "the Cratis way"; refresh when the AI repo
  changes.
- **P-29** Phase 2: **Samples source** — READMEs + curated sample files from `Cratis/Samples`, chunked
  whole-file with path headers, cited by GitHub URL.
- **P-30** Phase 2: **product-aware + client-language-aware retrieval** — boost/filter by product path prefix
  when the question names a product; prefer the asker's client language (C#/TS, later Kotlin/Elixir) variant
  pages.
- **P-31** Phase 3: **solved help-forum threads** as a source — only threads marked solved, authors stripped,
  channel notice + opt-out honored, cited as "community answer". Needs a decision record extending D-8 before
  any implementation.
- **P-32** Phase 3: **GitHub Discussions / answered issues** across product repos (public data, filtered to
  resolved).
- **P-33** **Docs-gap flywheel** — weekly digest of refusals + 👎 answers to a maintainer channel; later
  auto-file issues in the owning product repo. Prompter as a docs-coverage instrument.
- **P-34** **Docs MCP server** — expose `IPassages.Search` as an MCP tool alongside Chronicle.Mcp so Claude
  Code/Copilot/Cursor users share the bot's grounded retrieval.

## Open questions

- **Q-1** Chronicle dogfooding for the interaction log — needs a team ruling (D-6, recommendation: post-v1).
- **Q-2** Sonnet vs Haiku for generation — decide from eval results (P-18), not vibes; note Sonnet 5 intro
  pricing ends 2026-08-31.
- **Q-3** Is EU-region inference (Vertex/Bedrock) a requirement or a nice-to-have? Affects D-8 wiring only.
- **Q-4** Adopt Answer Overflow alongside Prompter (indexes solved threads into Google — complementary)?
- **Q-5** Where does Prompter's Pulumi code live — a workload entry in Studio's `Deployment/` stack
  (recommended, matches `studio-llm`/Prologue) or its own `Deployment/` project in this repo (D-11)?
- **Q-6** Does UpCloud Managed PostgreSQL support the `vector` extension? Only matters if in-cluster
  Postgres proves annoying (D-11 default is in-cluster).

## Parking lot (post-v1, not promised)

Confidence-gated chime-in on opted-in channels (threaded, per-channel enable, easy mute) · reranking
experiment (unverified benefit — measure first). (Docs-MCP, docs-gap digest, and language-awareness were
promoted into the content roadmap above.)
