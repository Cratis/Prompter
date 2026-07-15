# Backlog

The single consolidated work list. Milestone framing lives in [`V1_PLAN.md`](V1_PLAN.md); **the detailed
how/where/done-when for every P-item lives in [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md)**; decisions
in [`DECISIONS.md`](DECISIONS.md). Every item gets a P-number; open questions get Q-numbers; things we chose
not to promise live in the parking lot.

## M1 — Ingestion

- **P-01** ~~Validate the page inventory source against the real site~~ **Verified 2026-07-15**: `llms.txt`
  on cratis.io is only a pointer index (to `llms-small.txt`/`llms-full.txt`), so ingestion walks
  `sitemap-0.xml` (870 pages) and fetches each page's `.md` mirror (`<path>.md`, root → `index.md`) —
  implemented and spec-covered. Remaining residue: strip remaining MDX component tags (`<CardGrid>` etc.) from
  mirrors beyond the import lines already filtered, and evaluate `llms-full.txt` as a cheaper single-fetch
  alternative.
- **P-02** Batch embedding calls (Voyage accepts up to 128 inputs per request) instead of one call per chunk;
  respect rate limits with retry/backoff.
- **P-03** Make the ingestion exclusion list configurable (today only `client-snippets` is hardcoded).
- **P-04** Decide the schema-migration story once the schema changes for the first time (plain SQL file today;
  candidates: versioned SQL files à la Ada's `Database` project).
- **P-05** `prompter index` run in CI on a schedule as a fallback for the webhook (M5).

## M2 — Retrieval + Answering

- **P-06** Tune hybrid search: candidate pool size, RRF constant, and whether headings should be part of the
  `tsvector`. Measure against the golden set (P-17), don't guess.
- **P-07** Calibrate the refusal threshold (`Answering:MinScore`) on real questions — the default is a guess.
- **P-08** Consider a query-rewrite step (cheap model) for conversational questions before retrieval.
- **P-09** Enable Anthropic prompt caching for the system prompt + excerpt scaffold once traffic justifies it.
- **P-10** `prompter ask` should print confidence and passage provenance with a `--verbose` flag.

## M3 — Discord

- **P-11** Deferred responses for `/ask` — answering takes longer than Discord's 3-second interaction window;
  respond with a deferred callback, then follow up. (The scaffold returns the answer directly, which will time
  out on slow answers.)
- **P-12** Verify `GatewayClient.Id` correctly identifies the bot for mention detection (scaffold assumption),
  and handle role-mentions (`<@&…>`) and the nickname mention form (`<@!…>`).
- **P-13** Forum auto-reply: handler for new threads in `Discord:HelpForumChannelId`, answering as first reply.
- **P-14** Per-user rate limiting (e.g. 5 questions / 10 min) — protects LLM spend and the community's patience.
- **P-15** Split answers over 2000 chars into a thread instead of truncating with an ellipsis.
- **P-16** 👍/👎 feedback reactions on bot answers, recorded onto the interaction row.
- **P-17a** Register the Discord application, enable the Message Content intent, generate the invite URL with
  minimal permissions (Send Messages, Create Public Threads, Embed Links) — team action.

## M4 — Evaluation

- **P-17** Author a golden Q&A set (≥40 questions) spanning Chronicle, Arc, Fundamentals, Components, cli —
  sourced from real Discord history and the FAQ.
- **P-18** Eval harness scoring groundedness, citation correctness, and refusal behavior — crib
  `dotnet/eShopSupport`'s `AnswerScoringEvaluator` and `Microsoft.Extensions.AI.Evaluation`.
- **P-19** Wire the eval as a CI gate for prompt/retrieval changes (score regression fails the build).

## M5 — Operations

- **P-20** Re-index webhook: small HTTP endpoint (shared-secret protected) triggered by the Documentation
  repo's `build-docs` `repository_dispatch` chain, calling `IIndexer.Run()`.
- **P-21** Deploy: Hetzner CAX11, Docker Compose (bot + postgres), documented one-command deploy.
- **P-22** Retention purge job — schedule `IInteractionLog.PurgeExpired()` daily (RetentionDays default 90).
- **P-23** Privacy notice: pinned Discord message + docs page naming the bot, what it stores (hashed user IDs,
  questions/answers, 90 days), and the LLM subprocessor. See D-8.
- **P-24** Register Prompter in the `Documentation` repo: `PRODUCTS[]` entry in `web/scripts/sync-content.mjs`
  + sibling-clone list, so `Documentation/` here appears on cratis.io.
- **P-25** Run the `sync-copilot-instructions` workflow to pull the shared `.ai/` + `.claude/` + `.github`
  config from the AI repo (do not hand-copy rules).
- **P-26** Repo settings: create the GitHub repo `Cratis/Prompter`, add secrets `DOCKER_USERNAME`,
  `DOCKER_PASSWORD`, `PAT_DOCUMENTATION`; Docker Hub repo `cratis/prompter`.

## Open questions

- **Q-1** Chronicle dogfooding for the interaction log — needs a team ruling (D-6, recommendation: post-v1).
- **Q-2** Sonnet vs Haiku for generation — decide from eval results (P-18), not vibes; note Sonnet 5 intro
  pricing ends 2026-08-31.
- **Q-3** Is EU-region inference (Vertex/Bedrock) a requirement or a nice-to-have? Affects D-8 wiring only.
- **Q-4** Adopt Answer Overflow alongside Prompter (indexes solved threads into Google — complementary)?

## Parking lot (post-v1, not promised)

Confidence-gated chime-in on opted-in channels (threaded, per-channel enable, easy mute) · docs-MCP server
exposing `IPassages.Search` (pairs with Chronicle.Mcp) · weekly "unanswered questions" digest → docs coverage
gaps · reranking experiment (unverified benefit — measure first) · Kotlin/Elixir client docs emphasis tuning.
