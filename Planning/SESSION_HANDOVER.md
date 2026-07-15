# Session handover

Resume state for anyone (human or agent) continuing work in a fresh session. Newest entry first — append,
don't rewrite history.

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
