# Session handover

Resume state for anyone (human or agent) continuing work in a fresh session. Newest entry first — append,
don't rewrite history.

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
