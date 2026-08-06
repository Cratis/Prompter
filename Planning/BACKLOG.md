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
- **P-09** ~~Enable Anthropic prompt caching for the system prompt~~ **Done 2026-07-16**: the pinned
  `Anthropic` 12.35.1 exposes `AIContent.WithCacheControl(...)` (stored on `AdditionalProperties`, read back by
  the `AsIChatClient` path), so the system prompt is now built as an ephemeral-cacheable `TextContent`. No DI or
  config change. Note: at ~350 tokens the system prompt is below the model's minimum cacheable prefix, so it's a
  no-op today and begins paying off automatically as the prompt grows (D-5's "cents at current volume" holds).
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
- **P-12** ~~Mention hardening~~ + **M3.3 #ask channel** — **Done 2026-07-15** (code): pure
  `Mentions.ResolveQuestion(content, botId, isBot, channelId, askChannelId)` handles `<@id>` and `<@!id>`,
  ignores `<@&…>` role mentions / `@everyone` / bot authors / self, strips the mention to the question, and
  treats plain messages in `Discord:AskChannelId` as questions (other channels still require a mention).
  `GatewayClient.Id` confirmed populated from READY (no startup REST lookup needed). 13 facts.
  **Live test-server check is the M3.2/M3.3 done-when.**
- **P-13** ~~Forum auto-reply~~ **Done 2026-07-15** (code): `HelpForum` implements NetCord's
  `IGuildThreadCreateGatewayHandler` — on a newly-created thread whose `ParentId` matches
  `Discord:HelpForumChannelId`, it reads the starter message, answers as the first reply, then posts the standing
  "A human will follow up…" line (two sends so the 2000-char guarantee holds). Pure `ShouldAnswer` guard
  spec-covered (5 facts); auto-registered by assembly scan. **Live forum-post check is the M3.4 done-when.**
- **P-14** ~~Per-user rate limiting (e.g. 5 questions / 10 min)~~ **Done 2026-07-15** (logic): `RateLimiter`
  is a pure per-user token bucket (`TryConsume(userHash, now)`), config `Discord:RateLimit` (`MaxQuestions`
  5 / `WindowMinutes` 10), spec-covered (within-limit, exceed, window refill, partial refill, per-user
  isolation). **Wired 2026-07-16**: registered in `AddPrompter`; every entry point (mention, `#ask`, `/ask`,
  forum) calls `TryConsume(UserHash.For(id), TimeProvider.System.GetUtcNow())` before answering and sends a
  friendly `DiscordOptions.RateLimitedReply` when over limit (ephemeral for `/ask`). `WindowMinutes>0` is
  validated at startup (`ValidateOnStart`) so a zero-window misconfig fails fast instead of silently disabling
  limiting. **M3.8 resilience** landed with it: `answers.For` runs under a 60s `AnswerTimeoutSeconds`
  cancellation, and each handler has a catch-all that logs + posts `DiscordOptions.ErrorReply` instead of going
  silent — the gateway handlers can no longer throw.
- **P-15** ~~Split answers over 2000 chars instead of truncating~~ **Done 2026-07-15** (code): pure
  `DiscordAnswers.Split(Answer) : IReadOnlyList<string>` packs paragraphs greedily into ≤2000-char chunks (max 3,
  sources on the last), hard-splits oversized paragraphs, falls back to `Format` for short answers; `Mentions`
  sends each chunk in order. 23 facts, incl. code-fence safety (a fenced block is atomic; an oversized block
  hard-splits with balanced re-opened fences and its language hint preserved).
- **P-16** ~~👍/👎 feedback~~ **Done 2026-07-15** (code): switched from reactions to **buttons** (per
  `DISCORD_BEST_PRACTICES.md` — buttons carry the interaction + user id, no pre-add API calls, never fail
  silently). `v1_1_0.sql` adds `answer_message_id` + `feedback`; `IInteractionLog.Record` returns the row id,
  with `SetAnswerMessage`/`RecordFeedback`. 👍/👎 buttons attach to answers across `/ask`, mentions, and forum
  replies; a `Feedback` component-interaction handler parses the custom id (`fb:<verdict>:<id>`) and writes the
  verdict, acking ephemerally. Pure `FeedbackButton`/`FeedbackVerdicts` spec-covered (19 facts).
  **Live click-flips-the-row check is the M3.7 done-when.**
- **P-17a** Register the Discord application, enable the Message Content intent, generate the invite URL with
  minimal permissions (Send Messages, Create Public Threads, Embed Links) — team action.

## M4 — Evaluation

- **P-17** ~~Author a golden Q&A set (≥40 questions) spanning Chronicle, Arc, Fundamentals, Components, cli~~
  **Done 2026-07-15**: `Eval/golden-questions.yaml` (+ `Eval/README.md`) — **57 in-scope** questions across all
  five products + **12 out-of-scope refusals** (incl. adversarial near-misses like EventStoreDB/Marten, Kafka
  consumer groups). ~39 pages content-verified against the real `.md` mirrors; the rest sitemap-confirmed.
  Schema (id/product/type/question/expected/expected_pages/rationale) documents exactly how P-18 will score it.
- **P-18** ~~Eval harness scoring groundedness, citation correctness, and refusal behavior~~ **Done
  2026-07-15** (code): `Eval/Prompter.Eval.csproj` (dev-only, publish-excluded) parses the golden set
  (YamlDotNet), runs each question through `IAnswers`, and scores citation-hit (page-set intersection with
  `.md`/`/index` normalization), refusal-accuracy, and groundedness (`Microsoft.Extensions.AI.Evaluation`
  judge, cribbed from eShopSupport), writing markdown+JSON to `Eval/results/`. Pure scorers spec-covered (11
  facts). Run with `dotnet run --project Eval` once Voyage + Anthropic keys + a corpus exist.
- **P-19** ~~Wire the eval as a CI gate~~ **Done 2026-07-16** (scaffolding): `.github/workflows/eval.yml`
  runs on `workflow_dispatch` + PRs **labeled `eval`** only (unlabeled PRs skip → zero API spend), spins up a
  `pgvector/pgvector:pg17` service, indexes, runs the harness, uploads the report, and fails via
  `Eval/check-baseline.py` when any metric drops below `Eval/baseline.json` minus a tolerance. Secrets:
  `VOYAGE_API_KEY`, `ANTHROPIC_API_KEY`. **The baseline holds documented placeholders — regenerate from one
  real `dotnet run --project Eval` once keys exist** (steps in `Eval/README.md`), then it becomes a live gate.

## M5 — Operations

- **P-20** ~~Re-index webhook~~ **Done 2026-07-15** (code): bot mode is now a Kestrel `WebApplication`
  co-hosting the NetCord gateway + `GET /healthz` (DB `SELECT 1` + gateway `Ready`) and `POST /reindex`
  (`X-Reindex-Secret` compared with `CryptographicOperations.FixedTimeEquals`; 401 / 202-background-run /
  409-already-running; empty configured secret ⇒ refuse). `index`/`ask` stay console. `ReindexSecret` added to
  options; Dockerfile → `aspnet` base + `EXPOSE 8080`; also added `GatewayIntents.Guilds` so forum
  thread-create (P-13) events arrive. Pure `ReindexAuth`/`ReindexGate` spec-covered (10 facts); endpoints
  runtime-smoke-tested. **Wiring the Documentation build to call `/reindex` (+ ingress + k8s secret) is M5.3.**
- **P-21** Deploy: join the existing UpCloud UKS cluster per D-11/D-15. **Code shipped 2026-08-06** — a
  `Deployment/` Pulumi C# project (own stack, `file://./state`, passphrase secrets, Studio conventions
  throughout) provisions namespace + Postgres/pgvector StatefulSet + the bot Deployment/Service + an ingress
  route for `/reindex`, and `.github/workflows/deploy-production.yml` pins the image tag and runs `pulumi up`,
  called automatically from `publish.yml`. **What remains is credential/team work, not code:** the
  `PULUMI_CONFIG_PASSPHRASE`/`UPCLOUD_TOKEN` secrets on this repo, the runtime secrets set via
  `Deployment/scripts/set-secrets.sh`, a DNS record for the ingress host, and the first
  `pulumi stack init production` + `pulumi up` (see `Deployment/README.md`). Nothing in the stack has been
  applied yet — it is unrun infrastructure code.
- **P-22** ~~Retention purge job~~ **Done 2026-07-15** (code): `RetentionPurge : BackgroundService` sweeps on
  a 1-minute initial delay then daily (`PeriodicTimer`), calling `IInteractionLog.PurgeExpired` (deletes
  interactions older than `RetentionDays`, default 90, on the existing `occurred_at` column), logging the count
  and swallowing failures so the loop never dies. Registered in bot mode only. Cadence + resilience
  spec-covered (6 facts); the `DELETE` cutoff was live-verified against a throwaway Postgres.
- **P-23** Privacy notice: pinned Discord message + docs page naming the bot, what it processes (question text,
  sent to the LLM subprocessor and not retained) and what it stores (nothing identifying — no message content,
  no user id; only anonymous answer signal), and the LLM subprocessor. See D-13 (amending D-8);
  `Documentation/concepts/privacy.md` is the docs-page target already written.
- **P-24** Register Prompter in the `Documentation` repo: `PRODUCTS[]` entry in `web/scripts/sync-content.mjs`
  + sibling-clone list, so `Documentation/` here appears on cratis.io.
- **P-25** Run the `sync-copilot-instructions` workflow to pull the shared `.ai/` + `.claude/` + `.github`
  config from the AI repo (do not hand-copy rules).
- **P-26** ~~Repo settings~~ **Mostly done 2026-07-15**: `Cratis/Prompter` created (public, D-12) and pushed;
  secrets are **org-level** and confirmed reaching this repo (live `documentation.yml` dispatch succeeded;
  Chronicle.Mcp publishes with zero repo secrets). Residue, re-checked 2026-08-06: **no image has ever been
  published** — `hub.docker.com/v2/repositories/cratis/prompter` still 404s and `gh release list` is empty,
  because `cratis/release-action` only cuts a release for a merged PR labeled `major`/`minor`/`patch` and the
  one merged PR carried none. The first release is a `publish.yml` `workflow_dispatch` with an explicit
  version (or the next PR merged with a label) — see the release mechanics in
  [`DEPLOYMENT.md`](DEPLOYMENT.md). Deploy secrets `PULUMI_CONFIG_PASSPHRASE` + `UPCLOUD_TOKEN` must exist on
  this repo (or at org level) before `deploy-production.yml` can run.

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
- **P-33** **Docs-gap flywheel** — turn refusals and 👎 answers into docs work: a digest to a maintainer
  channel, and from there issues in the owning repo. Prompter as a docs-coverage instrument. There are two
  possible feeds, and choosing between them is
  [D-14](DECISIONS.md#d-14--storing-question-text--open--2026-08-06):
  - **Feed A — consent in the moment (via P-45, unblocked).** The asker clicks "this should be documented" on
    a refusal; the question text travels straight to the maintainer channel (later: an issue) and is never
    persisted. Nothing is stored, so [D-13](DECISIONS.md#d-13--interaction-log-stores-no-personal-data--2026-07-16)
    is untouched and no new decision record is needed.
  - **Feed B — mined question text (blocked).** Store question text on refusals behind a consent notice and a
    narrow retention window, which is what makes counting and clustering possible ("12 people asked this
    week"). Needs D-14 ruled first — today the interaction log holds `was_refusal` and a confidence and
    nothing else, so there is literally nothing to mine.
  Whichever feed, before anything is filed automatically it needs: **clustering** (twelve askers on one topic
  is one issue, not twelve), **product routing** to pick the target repo (the same classifier P-30 wants), a
  **rate cap**, and a human approval step. Digest-with-one-click-file first; full automation only once the
  signal proves clean. Which repo receives the issue is **Q-7**.
- **P-34** **Docs MCP server** — expose `IPassages.Search` as an MCP tool alongside Chronicle.Mcp so Claude
  Code/Copilot/Cursor users share the bot's grounded retrieval.

## Review follow-ups (2026-07-16)

From the whole-project review in [`REVIEW_2026-07-16.md`](REVIEW_2026-07-16.md) — full detail and file:line
there. Two High findings were already fixed on branch `review/2026-07-16-followups` (the reversible user-id
hash → keyed HMAC, and the lexical retrieval arm dropping its top matches). The refusal-threshold finding is
folded into **P-07**; the hybrid-tuning angle into **P-06**. Remaining actionable items:

- **P-35** ~~Hash the string that is actually embedded (title + heading path + content), not just the body~~
  **Done 2026-07-16** (branch `fix/format-preserve-sources`): `Chunk.EmbeddingInputFor`/`Chunk.EmbeddingInput`
  are now the single source of truth for the embedded string, hashed by the chunker and embedded by the
  indexer, so a title/heading rename re-embeds instead of being skipped as unchanged and the two can't drift.
  Specs: hash-covers-embedded-string invariant + heading-rename + title-rename each change the hash.
- **P-36** Derive answer citations from the model's `[n]` markers (map to `found[n-1].Page`), not the top-4
  retrieved pages — fixes miscitation and makes the `[n]` markers line up with the "Sources" list. **Deferred
  (needs live validation, out of the safe review-fix subset.)**
- **P-37** ~~Make `DiscordAnswers.Format` reserve room for the sources line and truncate only the body~~
  **Done 2026-07-16** (branch `fix/format-preserve-sources`, commit `735a2fc`).
- **P-38** ~~Validate `Voyage:Dimensions` against the `vector(1024)` schema at startup~~ **Done 2026-07-16**
  (branch `fix/format-preserve-sources`): `VoyageOptions.SchemaDimensions` (1024) + `DimensionsMatchSchema`
  wired into the shared `AddPrompter` `ValidateOnStart` chain; default matches so keyless CLI is unaffected.
- **P-39** ~~Guard `EnsureSchema` with a `pg_advisory_lock`~~ **Done 2026-07-16** (branch
  `fix/format-preserve-sources`): a session-level `pg_advisory_lock` held on a dedicated connection for the
  whole migration run serializes overlapping starts; the version insert is also `ON CONFLICT (version) DO
  NOTHING` as a second line of defense. Live-verified against Postgres (fresh apply 1.0.0→1.2.0, clean no-op
  second run, no lingering lock).
- **P-40** ~~Broaden retry classification to status-less transient failures~~ **Done 2026-07-16** (branch
  `fix/format-preserve-sources`): `EmbeddingRetry.IsTransient(null)` now retries (connection reset / DNS /
  socket timeout surfacing as a status-less `HttpRequestException`). Optional jitter / `Retry-After` and
  HttpClient-timeout (`TaskCanceledException`) retrying were **not** taken this pass — revisit if a live index
  run shows a need.
- **P-41** Enforce an in-scope answer-rate metric in the eval gate and regenerate `Eval/baseline.json` from a
  keyed run — otherwise over-refusal regressions pass CI. **Deferred (needs API keys, out of the safe subset.)**
- **P-42** ~~Add startup validation for `AnswerTimeoutSeconds > 0` and (bot mode) a non-empty `Discord.Token`;
  thread `ApplicationStopping` into the background reindex~~ **Done 2026-07-16** (branch
  `fix/format-preserve-sources`): the two startup validations landed (`DiscordOptions.AnswerTimeoutIsValid` in
  the shared chain; `TokenIsPresent` as a bot-mode-only validator in `Program.cs`), and the background reindex
  now runs under `IHostApplicationLifetime.ApplicationStopping` (cancels cleanly on shutdown, logged as a
  distinct outcome). Bundled the co-located review Low: `ReindexAuth` now SHA-256-hashes both secrets to a
  fixed 32 bytes before `FixedTimeEquals`, so the constant-time compare no longer leaks the secret's length.
- **P-43** Prune the stale agent worktrees + `worktree-agent-*` local branches (git hygiene). **Partly done
  2026-07-16**: all 19 `.claude/worktrees/agent-*` worktree **directories** were removed (all clean — nothing
  dirty discarded) and the registry pruned back to the single main checkout, clearing the disk clutter and the
  nested-worktree `MultipleGlobalAnalyzerKeys` hazard. **Left for the user:** the 19 `worktree-agent-*`
  **branches** are preserved. They are unmerged by ancestry (their work was cherry-picked onto `main` under new
  hashes, so `main` has the content but ancestry can't prove per-branch equivalence), so deleting them needs a
  force delete (`git branch -D`) — held back as a destructive step on branches this session didn't create.

## Prompter as a bridge to the trackers (2026-08-06)

The scope decided in [D-16](DECISIONS.md#d-16--prompter-bridges-discord-and-the-trackers--2026-08-06):
Prompter does not only *answer* — it moves work between the community and the Cratis issue trackers, in both
directions, for **every kind of work item** (bug, API gap, feature request, idea, docs gap). P-44/P-45/P-46
are the two directions plus the notification; P-47 is what happens to an issue once it exists.

## Post-v1 surfaces (2026-08-06)

- **P-44** ~~**GitHub issues surface**~~ **Done 2026-08-06** (code) — Prompter answers newly-opened issues on the Cratis product repos with
  the same grounded retrieval it uses on Discord. `IAnswers.For` is surface-agnostic, so this is a new entry
  point plus a webhook, not new answering logic: add `POST /github/webhook` to the Kestrel host that already
  serves `/healthz` + `/reindex`, verify the `X-Hub-Signature-256` HMAC the same constant-time way
  `ReindexAuth` does, answer on `issues.opened`, and post a cited comment. Rules: **silence on refusal** (no
  comment beats a hedging comment on an issue tracker), a visible "answered by Prompter — correct me" line,
  an opt-out label (`no-prompter`) honored per issue and per repo, and a per-repo rate cap. Start as a GitHub
  App installed on one repo behind a confidence threshold; a per-repo `issues.opened` workflow calling the
  endpoint is the cheaper spike if App registration is slow. Composes with **P-33**: a refusal on an issue
  *is* a docs gap already sitting in a tracker — label it `docs-gap` and the filing problem disappears.
  Distinct from **P-32**, which ingests *answered* issues as a retrieval source; the two compose.
  **Shipped:** `POST /github/webhook` verifies the `X-Hub-Signature-256` HMAC against the raw body, ignores
  everything that is not `issues.opened` (so a repository can point its whole webhook at it), skips bots and
  pull requests, honors the `no-prompter` label, and answers only for repositories on the opt-in allowlist —
  **staying silent on a refusal**, which is reported to the maintainer channel instead. The ingress publishes
  the path; `WebhookAuth`/`IssueEvents`/`IssueAnswerComment` are spec-covered. Live verification needs the
  deployed bot and a repository webhook (playbook Stage C3).
- **P-45** ~~**File a GitHub issue from Discord**~~ **Done 2026-08-06** (code) — turn a conversation into tracked work: a bug someone hit, an
  API that is missing, a feature request, a half-formed idea, or a documentation gap. Two entry points: a
  `/issue` slash command, and a **message context-menu action** ("File as issue") so an existing message or
  thread can be captured without retyping it. Prompter drafts the issue from the conversation — title, body,
  the type, and which repo it belongs in — shows it back as an **ephemeral preview with Confirm / Edit /
  Cancel**, and only then opens it. The click is the consent and nothing is persisted here, so
  [D-13](DECISIONS.md#d-13--interaction-log-stores-no-personal-data--2026-07-16) is untouched and
  [D-14](DECISIONS.md#d-14--storing-question-text--open--2026-08-06) stays unanswered.
  Rules: **anyone may file** (see D-16 — current volume does not justify an approval step, and the reporter
  is the person who knows the problem), issues carry a `from-discord` label and a link back to the thread so
  maintainers can follow up in context, the per-user cap reuses `RateLimiter`, and a similarity check against
  recent open issues offers "this looks like #123 — comment there instead?" before opening a duplicate.
  Routing is **Q-7**: the owning product repo, which needs the product classifier P-30 wants anyway; when the
  classifier is unsure, ask in the preview rather than guessing.
  **Shipped as the `/issue` command:** Prompter drafts title/body/kind/product with the model, routes to the
  owning repository, offers likely duplicates, and shows an ephemeral preview with Create/Cancel; the draft
  lives in memory for 15 minutes and is taken on click, so a double-click cannot file twice and an abandoned
  draft leaves no trace. `IssueRouting`/`IssueComposition`/`IssueDraftParsing`/`IssueButton`/`IssuePreview`/
  `PendingIssues` are spec-covered. **Residue:** the message context-menu entry point ("File as issue" on an
  existing message) needs NetCord's message-command context registered, which was not compile-verifiable
  against beta.12 in the same pass — the slash command covers the same ground meanwhile. A refusal or a 👎
  offering the same action pre-filled (the P-33 flywheel) is likewise still to come.
- **P-46** ~~**Tell Discord when a GitHub issue is opened**~~ **Done 2026-08-06** (code) — maintainers should see tracker activity where they
  already are. **Do the zero-code version first:** a Discord channel webhook URL with `/github` appended,
  registered as a repo (or org) webhook for `issues` events — no Prompter involvement, working in minutes,
  and it stays useful even if Prompter is down. Build it *into* Prompter (on top of P-44's webhook receiver)
  only for what the native version cannot do — which is what shipped: `IssueNotification` posts to
  `GitHub:NotifyChannelId` saying whether Prompter answered the issue from the docs or could not, which is the
  line that turns a notification into triage. Spec-covered. Per-product channel routing is not built; one
  channel today.

- **P-47** **Auto-implement the easy ones** — an issue that is genuinely mechanical (a typo, a missing null
  guard, a doc page that should exist, a small API addition with an obvious shape) should not wait for a
  maintainer's evening. **Prompter does not run coding agents** — GitHub's own do: assign the issue to
  Copilot's coding agent, or run a Claude Code GitHub Action on `issues.labeled`. Prompter's part is only to
  file issues good enough to act on, and to carry the label when a human asks for it.
  Non-negotiable guardrails: a **human applies the label** (never Prompter, never from a Discord message),
  the agent opens a **draft PR** and never merges, the normal build/spec/eval gates apply unchanged, and it
  runs only on repos that opt in. Start where a wrong call is cheapest — documentation content and
  single-file fixes — and widen once the PRs are actually good. The classification is the hard part, not the
  plumbing: "no-brainer" judged wrong spends maintainer review time, which is the resource this is meant to
  save. Depends on P-45 (issues worth acting on) and needs a decision record before any repo opts in.

## Open questions

- **Q-1** Chronicle dogfooding for the interaction log — needs a team ruling (D-6, recommendation: post-v1).
- **Q-2** Sonnet vs Haiku for generation — decide from eval results (P-18), not vibes; note Sonnet 5 intro
  pricing ends 2026-08-31.
- **Q-3** Is EU-region inference (Vertex/Bedrock) a requirement or a nice-to-have? Affects D-8 wiring only.
- **Q-4** Adopt Answer Overflow alongside Prompter (indexes solved threads into Google — complementary)?
- ~~**Q-5** Where does Prompter's Pulumi code live~~ — **answered 2026-08-06 by
  [D-15](DECISIONS.md#d-15--prompters-pulumi-stack-lives-in-this-repo--open--2026-08-06)**
  (own `Deployment/` project here, own stack, deploying into the *existing* cluster). Reading Studio's actual
  deployment code flipped D-11's recommendation — see D-15 for the evidence. Confirm with the team; the
  resource code ports to Studio's stack nearly verbatim if they disagree.
- **Q-6** Does UpCloud Managed PostgreSQL support the `vector` extension? Only matters if in-cluster
  Postgres proves annoying (D-11/D-15 default is in-cluster).
- ~~**Q-7** Where do issues filed from Discord get filed~~ — **answered 2026-08-06: the owning product
  repo**, routed by the product classifier (Chronicle, Arc, Fundamentals, Components, cli, Documentation).
  That makes **P-30**'s classifier a dependency of P-45 rather than a nice-to-have, and means the preview
  step must let the filer correct the repo when the classifier is unsure.

## Parking lot (post-v1, not promised)

Confidence-gated chime-in on opted-in channels (threaded, per-channel enable, easy mute) · reranking
experiment (unverified benefit — measure first). (Docs-MCP, docs-gap digest, and language-awareness were
promoted into the content roadmap above.)
