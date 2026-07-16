# Session handover

Resume state for anyone (human or agent) continuing work in a fresh session. Newest entry first — append,
don't rewrite history.

## 2026-07-16 — Review follow-ups: safe subset (P-35, P-37, P-38, P-39, P-40, P-42) on a branch

**State:** Branch **`fix/format-preserve-sources`** off `main` @ `99ab61f`, **not pushed / not merged**. Release
build **0 warnings**, **278 specs green** (up from 260 on `main`; the branch adds P-37's specs plus the new
review-fix specs). Six commits sit on top of `main`:

- `735a2fc` **P-37** — `DiscordAnswers.Format` keeps citations on long single-message answers (prior session).
- `4db49ba` **P-35** — hash the embedded composite (title + heading path + content), not just the body.
  `Chunk.EmbeddingInputFor`/`Chunk.EmbeddingInput` are the single source of truth shared by the chunker (hashes
  it) and indexer (embeds it), so a title/heading rename re-embeds instead of being skipped as unchanged.
- `39e1a84` **P-38** — validate `Voyage:Dimensions` against the fixed `vector(1024)` schema at startup
  (`VoyageOptions.SchemaDimensions` + `DimensionsMatchSchema` in the shared `ValidateOnStart` chain).
- `2977a7b` **P-42 (partial)** — startup validation for `AnswerTimeoutSeconds > 0` (shared chain) and a
  non-empty `Discord.Token` (bot-mode-only validator in `Program.cs`, so keyless CLI still passes).
- `bc9e1da` **P-39** — session `pg_advisory_lock` (held on a dedicated connection) serializes overlapping
  migration starts; version insert is also `ON CONFLICT (version) DO NOTHING`. **Live-verified** on Postgres:
  fresh `index` applies 1.0.0→1.2.0, a second run is a clean no-op, no advisory lock lingers, and both runs stop
  at the expected keyless Voyage 401.
- `bed7b45` **P-40** — `EmbeddingRetry.IsTransient(null)` now retries status-less network faults (connection
  reset / DNS / socket timeout).

**Deliberately excluded from this pass:** **P-36** (changes citation behavior — needs live validation) and
**P-41** (eval baseline — needs API keys). **Residual on P-42:** threading `ApplicationStopping` into the
background reindex is still open. **P-40** optional extras (jitter, `Retry-After`, retrying HttpClient
`TaskCanceledException` timeouts) were not taken. **P-43** (worktree hygiene) untouched.

**Next:** decide whether to merge `fix/format-preserve-sources` into `main` + push (a review follow-up,
externally visible on public `main`). Then P-07 calibration once keys land.

## 2026-07-16 — Interaction log minimized to zero personal data (D-13)

**State:** Branch **`privacy/minimal-interaction-log`** off `main`, **not pushed**. Release build **0 warnings**,
**260 specs green**. The migration chain was live-verified against a fresh Postgres (`docker compose up -d` →
`dotnet run -- index`): all three migrations apply (1.0.0 → 1.1.0 → 1.2.0) and the `interactions` table ends up
as exactly `id, occurred_at, source, cited_pages, confidence, was_refusal, feedback` — the run then fails at the
expected keyless Voyage boundary, confirming schema + DI/startup are intact.

**What changed (decision [D-13](DECISIONS.md), amending D-8):** the interaction log now stores **no personal
data** — a `v1_2_0` migration drops `question`, `answer`, `user_hash`, and `answer_message_id`, leaving only
anonymous signal (`source`, `cited_pages`, `confidence`, `was_refusal`, `feedback`). `IAnswers.For` and
`Interaction` no longer carry user/content; `IInteractionLog.SetAnswerMessage` is gone. Raw Discord user IDs are
scrubbed from the operational logs. Rate limiting keeps an **in-memory-only** key (the raw id, never persisted or
logged), so the whole `UserHash`/keyed-hash + mandatory `UserHashKey` from the previous session is removed
(supersedes that part of the prior entry). Privacy notice (`Documentation/concepts/privacy.md`), FAQ, and
architecture doc rewritten to "we keep no message content and nothing that identifies you".

**Follow-on:** the docs-gap flywheel (BACKLOG P-33) is now explicitly **blocked** on re-introducing question text
behind its own consent/retention decision. The retention purge stays as housekeeping (not a privacy control).

**Next:** decide whether to merge/push this branch. Then P-07 calibration when keys land.

## 2026-07-16 — Fresh whole-project review + two High fixes (branch, not merged)

**State:** Branch **`review/2026-07-16-followups`** off `main` @ `e9f68ad`, **not pushed / not merged**.
Release build **0 warnings**, **263 specs green** (up from 260; +3 for the keyed hash). A fresh whole-project
review (four independent subsystem passes + a manual core read) is recorded in
[`REVIEW_2026-07-16.md`](REVIEW_2026-07-16.md).

**Fixed on the branch (two High):**
- **Reversible user-id hash → keyed HMAC** (`UserHash`): bare `SHA256("prompter:{snowflake}")` over a public,
  enumerable id in a public repo was reversible by anyone holding the interaction log — defeating D-8. Now
  **HMAC-SHA256** keyed with `Cratis:Prompter:Discord:UserHashKey`, **required at startup when a Discord token
  is set** (CLI `index`/`ask` modes without a token are unaffected). New `for_UserHash` specs; documented in the
  README table + deployment secret list.
- **Lexical retrieval arm dropped its top matches** (`Passages`): the lexical CTE's `LIMIT` had no
  statement-level `ORDER BY`, so it kept an arbitrary 20 rows whenever >20 chunks matched. Added
  `ORDER BY ts_rank_cd(...) DESC` before the `LIMIT`. Not spec-coverable without a live DB; the M2.2
  calibration run exercises it.

**Logged, not fixed:** the rest of the review is in `REVIEW_2026-07-16.md` and mapped to `BACKLOG.md` — the
refusal-threshold design concern folds into **P-07**, hybrid tuning into **P-06**, and new items **P-35…P-43**
cover content-hash coverage, model-`[n]` citations, `Format` dropping sources on long answers, `Voyage:Dimensions`
validation, migration advisory lock, retry classification, the eval answer-rate gate, startup validation, and
worktree hygiene. Feedback-button routing under NetCord beta.11 still needs the token-gated test-server check.

**Next:** decide whether to merge/push this branch (a review follow-up, externally visible on public `main`),
then continue with P-07 calibration once keys land — where the threshold and lexical fixes both get their live
proof-out.

## 2026-07-16 — v1 code-complete: full M1–M5 build, two review passes, docs reconciled

**State:** `main` @ `a1159cb`, pushed, **260 specs green, 0 warnings** (Release). The `aspnet`-base Docker
image builds end-to-end (verified). **v1 is code-complete** — every M1–M5 feature is implemented and
spec-verified; what remains is live-key / test-server / deploy-gated (see "What's left"). Built via the
autonomous multi-agent loop (isolated worktrees; I integrate + run the authoritative Release build+tests on
real `main` before each commit).

**Shipped + integrated this run (on top of the prior M1/M2.1 work):**
- **M2:** P-09 prompt caching (system prompt marked ephemeral-cacheable via `Anthropic` 12.35.1's
  `WithCacheControl`; no-op until the prompt exceeds the model's min cache size, then automatic).
- **M3 (complete):** P-11 deferred `/ask`, P-12 mention hardening, M3.3 `#ask` channel, P-13 forum auto-reply,
  P-14 rate limiting **wired** into every entry point, P-15 long-answer splitting (+ code-fence + surrogate
  safety), P-16 feedback as **buttons** (not reactions; `v1_1_0` migration + component handler), M3.8
  resilience (60s timeout + catch-all apology; handlers never throw). NetCord beta.11 APIs verified against
  the shipped assemblies.
- **M4:** P-17 golden set (69 Qs), P-18 eval harness (`Eval/Prompter.Eval.csproj`), P-19 labeled-PR eval CI
  gate (`eval.yml` + `baseline.json` placeholder + `check-baseline.py`).
- **M5:** P-20 re-index webhook + `/healthz` (bot mode is a Kestrel `WebApplication` co-hosting the gateway;
  `Guilds` intent added for thread-create), P-22 retention purge job.
- **Docs/hygiene:** `DISCORD_BEST_PRACTICES.md`, three `Documentation/guides/*`, reconciled
  `DISCORD_INTEGRATION.md` + `V1_PLAN.md` + `IMPLEMENTATION_PLAN.md` + `README.md` to the shipped reality, and
  added a `.dockerignore` + gitignored `.claude/worktrees/`.

**Review passes (3 total) — all confirmed findings fixed:** (1) a **high-severity corpus-wipe** (empty crawl
deleted the whole `chunks` table — now guarded); (2) medium error-path bugs — audit writes moved out of the
answer `try` so a post-delivery failure no longer false-apologizes, `RetentionDays>0` validated at startup
(a `0` wiped the interactions table), feedback write guarded; (3) a **final integration/runtime audit**
(DI graph decompiled against NetCord internals) found the composition root complete and correctly-lifetimed,
migrations-before-serving, and interaction dispatch fully wired — **no startup/resolution/config-binding
defects**.

**Known low-priority item (documented, not fixed):** the typed `HttpClient`s for `VoyageEmbeddings`/`DocsSite`
are consumed by singletons, so `IHttpClientFactory` handler rotation never occurs — irrelevant for a
single-instance bot on stable DNS, but if ever multi-instance / long-uptime-DNS-sensitive, inject
`IHttpClientFactory` or set `PooledConnectionLifetime`. Not worth an unattended refactor.

**Decisions this run:** D-4 (Postgres+pgvector) **reaffirmed** (a "do we need SQL?" review kept it over SQLite
for the already-built hybrid RRF + cluster fit). Cratis/Chronicle dogfooding → recommended **post-v1** (D-6's
path: v1 keeps the `IInteractionLog` seam; Chronicle-backed log + Studio dashboards is the flagship post-v1
milestone). Default answer model `claude-sonnet-5` confirmed a valid current id.

**What's left for v1 (all key / test-server / team-gated — nothing further is code-blocked):**
1. **Keys** — a Voyage + Anthropic key to: run the live full-corpus index (M1 done-when), the `ask --verbose`
   grounded run (M2.1), **M2.2 threshold calibration** (P-07 — set `Answering:MinScore` from ~25 probes), and
   generate the real `Eval/baseline.json` (then P-19 becomes a live gate).
2. **Discord token + test server** — the runtime checks for deferred `/ask`, mentions, `#ask`, forum reply,
   feedback buttons, rate-limit refusal, and the resilience apology.
3. **Team/deploy** — P-21 deploy to the UpCloud UKS cluster (D-11, resolve Q-5), P-17a Discord app
   registration, P-23 privacy notice (docs page exists; pin it), P-24/25/26 (Documentation registration, sync
   `.ai/` config, Docker Hub repo). P-06 hybrid tuning and P-08 query-rewrite stay deferred until calibration
   (P-07) shows a need.

**Gotcha (unchanged):** builds run *inside* an agent worktree under `.claude/worktrees/` fail with
`MultipleGlobalAnalyzerKeys` (two `.globalconfig` on the SDK's up-tree walk). Agents verify out-of-tree; the
authoritative gate is `dotnet build/test -c Release Prompter.slnx` on real `main`. Don't "fix" it in-repo.

## 2026-07-15 — Multi-agent push through M2–M5 (autonomous integration)

**State:** `main` @ `0723d78`, pushed, **197 specs green, 0 warnings**. Running an autonomous multi-agent loop
(isolated worktrees, I integrate + gate on real `main`). **Done + integrated this run:** P-04 versioned SQL
migrations (live-verified on Postgres), P-11 deferred `/ask`, P-13 forum auto-reply, P-15 long-answer splitting
(incl. code-fence safety), P-17 golden eval set (69 Qs), P-12 mention hardening + M3.3 `#ask` channel, P-20
re-index webhook + `/healthz` (bot mode is now a Kestrel `WebApplication` co-hosting the gateway) + `Guilds`
intent. Also added `Planning/DISCORD_BEST_PRACTICES.md` and `Documentation/guides/{using-prompter,discord-setup,
deploying}.md`. **Still running:** P-18 eval harness project, P-16 feedback buttons (+ `v1_1_0` migration).

**Decisions from the team this run:** D-4 (Postgres+pgvector) **reaffirmed** — a "do we need SQL?" review
landed on keep (SQLite was the alternative; kept for the already-built hybrid RRF + cluster fit). Cratis/Chronicle
dogfooding: recommendation is **post-v1** — v1 keeps the `IInteractionLog` seam (D-6), Chronicle-backed log +
Studio dashboards become the flagship post-v1 milestone. (Record these formally as a D-4 note + D-6 ruling when
convenient; not yet done.)

**Critical gotcha — nested-worktree builds:** agent worktrees live at `.claude/worktrees/…` INSIDE the repo, so
the SDK's up-tree `.globalconfig` discovery finds two configs → `MultipleGlobalAnalyzerKeys` fails any build run
*inside* a worktree. Agents verify out-of-tree; the **authoritative gate is `dotnet build/test -c Release
Prompter.slnx` on real `main`** (single config), which I run after every cherry-pick. Do not "fix" this in the
repo — a normal checkout/CI has one config.

**M3 live-verification residual (unchanged):** all Discord runtime paths (deferred `/ask`, mentions, forum
reply, feedback buttons) are code-complete + NetCord-beta.11-API-verified but need a **test server + Discord
token**; retrieval/answering + eval still need **Voyage + Anthropic** keys. Next disjoint waves after P-16/P-18:
P-22 retention job, P-08 query-rewrite, P-09 prompt caching, M3.8 resilience wrap.

## 2026-07-15 — Docs get the Starlight treatment; org secrets confirmed working

**State:** Landing pages upgraded to **MDX with the Documentation repo's components** (verified against
`Documentation/web/src/components/` source and cli's `index.mdx` as the reference): front door =
`TopicHero` + `SimpleCard` grid; getting-started = tutorial chapter (`YouWillLearn`, `Steps`, `Tabs` for the
four summon surfaces, `Recap`); section landings = card grids. Content pages remain `.md`. Icons chosen only
from names already used across the site (+`discord` from Starlight's builtin set). `sync-content.mjs`
processes product-repo `.mdx` (verified in its source). **Assumption recorded:** site-absolute links use the
`/prompter/` slug — must match the P-24 `PRODUCTS[]` registration; visual QA (the `qa-cratis-docs` skill)
happens once registered. Lint 0 errors; all relative links/toc hrefs verified.

**Secrets test result (P-26 effectively closed):** the `documentation.yml` dispatch on this repo completed
**success** — org-level `PAT_DOCUMENTATION` reaches Prompter, so org secret visibility includes this repo and
`DOCKER_USERNAME`/`DOCKER_PASSWORD` (same org level, proven by secret-less Chronicle.Mcp publishing) will
too. Remaining P-26 residue: only the Docker Hub `cratis/prompter` repository if pushes don't auto-create it.

## 2026-07-15 — Documentation restructured to the Cratis product shape + staging ladder

**State:** `Documentation/` now follows the product-docs conventions: Getting started / Guides / Concepts /
Reference buckets with per-folder `toc.yml` + `index.md`, a front-door index (one-sentence definition,
without/with framing), `why-prompter.md`, `grounded-answers.md`, `privacy.md` (doubles as the P-23 privacy
notice target), `running-locally.md`, `configuration.md`, `faq.md`. Org-standard `.markdownlint.json` added
(the missing piece that made `verify-markdown.sh` fail on defaults); markdownlint 0 errors, all internal
links/toc hrefs verified, external links return 200. `DEPLOYMENT.md` gained the **staging ladder** (Stage 0
laptop → optional Stage 1 simple UpCloud VM → Stage 2 D-11 cluster) — the bot dials out, so the laptop is a
legitimate try-out stage. **Secrets finding:** `DOCKER_USERNAME`/`DOCKER_PASSWORD`/`PAT_DOCUMENTATION` are
**org-level** (Chronicle.Mcp/cli have no repo secrets yet their workflows pass) — P-26 likely needs no new
secrets, at most an org admin confirming visibility includes Prompter; a live `documentation.yml` dispatch
test is queued. Note: `main` is several commits ahead of `origin/main` (incl. M2.1) — push pending the
user's go-ahead.

## 2026-07-15 — Doc retarget cleanup + M2.1 (`ask --verbose`) code-complete

**State:** Release build **zero warnings**, **113 specs green** (up from 88). Two commits added on `main`:
`5b043cc` (doc retarget — already on `origin/main` as an ancestor of the team's `f6d88f6`) and `ad43e54`
(M2.1 feature — **the one commit not yet pushed**; see "Push decision" below).

**What shipped:**

- **Deployment-doc cleanup (D-11 propagation)** — the review findings from `d193dfe` are cleared:
  `IMPLEMENTATION_PLAN.md` M5.3, `V1_PLAN.md` (M5 row + the old "one box / ≤€15/mo" line), and
  `RESEARCH.md`'s Hetzner run-cost now all point at the shared UpCloud UKS cluster; the DECISIONS.md reorder
  (D-11 after D-10) + marginal-cost note landed via the team's `c41f7de`/`f6d88f6`. Verified `no-svg1` is a
  real UpCloud Norway zone (Stavanger/Rennesøy), so the D-8 "data stays in Norway" claim holds.
- **M2.1 `ask --verbose` (P-10)** — new pure `Cli` layer: `AskArguments` (position-independent `--verbose`/
  `-v` parse) and `AskOutput` (renders the retrieved passages — score/page/heading, best first — before the
  answer, and returns exit code 1 on a refusal). `Answer` now carries the `Passages` it was grounded in
  (`Answer.Refusal` takes them too); this also feeds M4 groundedness scoring later. 9 new spec files
  (`for_AskArguments`, `for_AskOutput`) cover parse, render, pluralization, the empty-passages branch, and
  locale-stable (`InvariantCulture`) score formatting.

**Still blocked on keys (unchanged):** the live done-whens for M1 (full index run) **and** M2.1 (the
`--verbose` run against the real corpus) both need a **Voyage** key; M2.1's answer path also needs an
**Anthropic** key. Note `Passages.Search` embeds the query *first*, so even a keyless empty-corpus `ask`
cannot reach the refusal path — there is no keyless live smoke test.

**Push decision (awaiting the user):** `ad43e54` (M2.1) is committed locally but **not pushed** — the repo
is now public and pushing to `main` is externally visible, so it was left for the user to confirm (push
direct to `main`, or open a PR — mind that Publish triggers on merge and needs Docker Hub secrets first).

**Next actions, in order:**

1. Decide/push `ad43e54` (above).
2. When keys land: `docker compose up -d` → `dotnet run -- index` (closes M1.1+M1.4/M1), then
   `dotnet run -- ask "How do I append an event in Chronicle?" --verbose` (closes M2.1's live done-when).
3. **M2.2** threshold calibration (P-07): run ~20 in-scope + ~5 out-of-scope probes, set `Answering:MinScore`,
   record findings in `IMPLEMENTATION_PLAN.md` under a Calibration note.

## 2026-07-15 — Public on GitHub: Cratis/Prompter created and pushed

**State:** D-12 ruled **public** by the team; `https://github.com/Cratis/Prompter` created (public, MIT) and
`main` pushed with all history. Remaining from P-26: repo **secrets** (`DOCKER_USERNAME`, `DOCKER_PASSWORD`,
`PAT_DOCUMENTATION`) and the Docker Hub `cratis/prompter` repository — team actions. P-25
(`sync-copilot-instructions` workflow_dispatch to pull the shared `.ai/` config) is now unblocked — confirm
the correct `source_repository` value with the team (likely the AI repo) before dispatching. Note: the
Publish workflow triggers on merged PRs — the first merge to main will attempt a release; make sure Docker
Hub secrets exist first or it will fail (harmlessly).

## 2026-07-15 — Planning: deployment retargeted to the UpCloud cluster (D-11)

**State:** No code changes. New fact from the team: Cratis runs Studio on an **UpCloud UKS cluster**
(`no-svg1`, Norway) deployed via Pulumi C# with in-repo state — see `Studio/Deployment/` and
`Studio/Documentation/deployment/` (the reference implementation, incl. `deploy-production.yml`'s
version-pinning flow). [`DEPLOYMENT.md`](DEPLOYMENT.md) is rewritten around joining that cluster
(bot workload + in-cluster Postgres/pgvector with object-storage backups, mirroring the MongoDB precedent);
D-11 records the decision; the Hetzner plan is superseded (compose stays for local dev only). Open before
M5.3: **Q-5** (Pulumi code in Studio's stack — recommended — vs. this repo) and **Q-6** (managed-Postgres
pgvector support, only if in-cluster annoys). P-21/P-26 updated accordingly.

## 2026-07-15 — Planning: content & freshness design added

**State:** No code changes. Added [`CONTENT_AND_FRESHNESS.md`](CONTENT_AND_FRESHNESS.md) — the knowledge
design the plan was missing: the app-vs-corpus-vs-model mental model (docs deploys trigger a **re-index**,
never an app redeploy), the freshness architecture (event-driven `/reindex` from the Documentation deploy +
nightly safety net), the phased content-source roadmap (Phase 2: release notes, glossary grounding, Samples;
Phase 3: solved forum threads with consent, GitHub Discussions), and the ecosystem enhancements
(product-aware retrieval, docs-gap flywheel, docs-MCP server). `BACKLOG.md` gained P-27…P-34 for these;
docs-MCP/digest/language-awareness were promoted out of the parking lot. Note for P-31 (forum-thread
ingestion): requires a decision record extending D-8 before implementation.

## 2026-07-15 — Initial commits + M1 ingestion (code-complete, live run pending key)

**State:** The repo is now committed (four initial commits: scaffolding → source → specs → planning; **not
pushed** — `Cratis/Prompter` still does not exist, GitHub was left out of scope this session). All four **M1
tasks are implemented and verified by build + specs** (Release build **zero warnings**, **88 specs green**, up
from 29). The one thing not done live is the full real index run — it needs a **Voyage API key**, which is
still not configured.

**What shipped (M1):**

- **M1.2 Configurable exclusions** — `IngestionOptions.ExcludedPathSegments` (defaults `client-snippets`,
  `api-reference`) on `PrompterOptions.Ingestion`; `DocsSite.ParsePageUrls` now takes the list as a parameter
  and `DocsSite` gets `IOptions<PrompterOptions>` injected. Custom + default exclusions spec-covered. **Done.**
- **M1.3 MDX component stripping** — `MarkdownChunker.StripMdxComponents` (a code-fence-aware pre-pass) strips
  module imports, JSX `{/* … */}` comments and block-level component tags (paired, self-closing, and
  multi-line) while keeping the prose children of paired tags (hero text, card bodies). Import stripping moved
  out of `SplitIntoSections` into this pass. Specced against **real `index.md` + `arc.md` mirror fixtures**
  embedded in the Specs project (`Specs/Fixtures/`). **Done.**
- **M1.1 Batch embeddings + retry** — `Indexer` now buffers changed chunks across pages and embeds them in
  batches of `Voyage:BatchSize` (default **128**), upserting per batch, with a character-budget guard.
  `ResilientEmbeddingGenerator` (a decorator around `VoyageEmbeddings`) retries 429/5xx with exponential
  backoff; the pure policy is `EmbeddingRetry` (`IsTransient` / `BackoffFor`), fully spec-covered. Batching is
  spec-verified with fakes (`Specs/Fakes/`). **Code done; live full-corpus run is the remaining "done-when",
  blocked on the Voyage key.**
- **M1.4 Index-run summary as data** — `IIndexer.Run` returns an `IndexRun` record (pages, embedded,
  unchanged, removed, duration); `index` mode prints a one-line summary. `IndexRun` fields spec-verified via
  the Indexer specs. **Code done; the printed line will appear once a real run completes (needs key).**

**Next actions, in order:**

1. **Get a free Voyage API key** → set `Cratis__Prompter__Voyage__ApiKey` (or `appsettings.Development.json`).
2. `docker compose up -d` → `cd Source && dotnet run -- index`. Confirm: full run completes, prints the
   summary line, and a **second run reports `0 embedded` / all unchanged** (M1.1 + M1.4 done-when). Then M1 is
   fully closed.
3. Start **M2** (Retrieval + Answering) — needs the indexed corpus + an **Anthropic key**. First task is the
   `ask --verbose` UX (M2.1 in `IMPLEMENTATION_PLAN.md`).
4. When ready to go public: create `Cratis/Prompter`, push the four+ commits, then run
   `sync-copilot-instructions` (P-25) to pull the shared `.ai/` config (this supersedes CLAUDE.md's
   conventions section — expected).

**New gotchas / notes from this session:**

- **Voyage limits (verified against docs, 2026-07-15):** the plan's "128 inputs" was outdated — voyage-4
  actually allows **1,000 inputs and 320K tokens per request**. 128 is kept as a conservative, resilient
  default (128 × ≤4,100 chars ≈ ~175K tokens, well under the cap). Tunable via `Voyage:BatchSize`.
- **Embedding DI pattern changed:** `VoyageEmbeddings` is now registered as a typed `HttpClient` client and
  wrapped by `ResilientEmbeddingGenerator` (the `IEmbeddingGenerator` singleton). Swapping the embedder means
  changing what the decorator wraps.
- **MDX stripping is block-level** (component tags must start a line — true for every tag in the real
  mirrors). Inline mid-line components are intentionally left alone to avoid nuking inline generics like
  `List<T>`; note if a future page uses them. Self-closing component **attribute prose** (e.g. `<LinkCard
  description="…"/>`) is dropped with the tag — acceptable for the navigational landing page; revisit if it
  matters.
- **cratis.io → www.cratis.io 301:** the markdown mirrors 301-redirect to `www`; `HttpClient` follows it, so
  live ingestion is unaffected.
- Running `dotnet build`/`dotnet test` with `&&`-chaining hit a cwd reset here — run them as separate
  commands (or pass the `.slnx` path explicitly).

## 2026-07-15 — M0 shipped: scaffold complete and verified

**State:** Milestone M0 is done. The solution builds with **zero warnings in Release**, all **29 specs pass**,
and the ingestion pipeline was smoke-tested live: `docker compose up -d` + `dotnet run -- index` created the
pgvector schema on real Postgres, fetched cratis.io's `sitemap-0.xml` (870 pages), fetched real `.md` page
mirrors, chunked them, and stopped exactly at the Voyage embeddings call with 401 — the expected boundary,
since no API keys are configured yet.

**Nothing is committed** — the repo is `git init`-ed with everything untracked; the GitHub repo
`Cratis/Prompter` does not exist yet.

**Resume by reading, in order:** [`README.md`](README.md) (this folder's index) →
[`V1_PLAN.md`](V1_PLAN.md) → [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) (start at M1) →
[`DECISIONS.md`](DECISIONS.md) (do not re-litigate).

**Next actions, in order:**

1. Initial commit(s) per the Cratis git-commit conventions, create `Cratis/Prompter`, push (P-26 partially).
2. Get a Voyage API key (free) → run the first full index → start **M1** in
   [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) (P-02 batching first — a full run without batching makes
   ~one HTTP call per chunk; it works but is slow and rate-limit-prone).
3. Team inputs needed soon: D-6 ruling (Chronicle dogfooding — recommendation: defer), Discord app
   registration per [`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md) (needed at M3), Anthropic key (M2).

**Environment notes / gotchas:**

- SDK pinned `10.0.301` (`global.json`); local machines here also have 10.0.200/203 — `rollForward:
  latestFeature` handles it. macOS has no `timeout` command (a smoke-test annoyance, not a code concern).
- **NetCord is pre-1.0** (`1.0.0-beta.11` pinned). Gateway/mention wiring compiles but the runtime behavior
  (esp. `GatewayClient.Id` for self-mention detection and the `/ask` deferral API) is **unverified** — first
  M3 task is verifying against a test server. Fallback recorded in D-3: Discord.Net 3.20.x.
- `MA0136` is disabled in `.globalconfig` (raw strings for SQL/fixtures — deliberate, documented inline).
- `.editorconfig`/`.globalconfig`/`.gitignore`/`.gitattributes`/`LICENSE` are copied from `Chronicle.Mcp`
  (the template repo for conventions). The shared `.ai/` config is NOT here yet — pull it via the
  `sync-copilot-instructions` workflow after the GitHub repo exists (P-25); meanwhile the sibling `cli` repo's
  `.claude/` rules are the reference.
- Ingestion reality (verified live 2026-07-15): cratis.io `llms.txt` is only a pointer file → we walk
  `sitemap-0.xml` and fetch `<path>.md` mirrors (root → `index.md`); `api-reference` + `client-snippets`
  excluded; MDX `import` lines stripped by the chunker; remaining MDX component tags are M1 task 3.
- Local dev loop: `docker compose up -d` → `cd Source` → `dotnet run -- index` → `dotnet run -- ask "…"` →
  `dotnet run` (bot). Quality gates: `dotnet build -c Release` (must be zero warnings) + `dotnet test`.
