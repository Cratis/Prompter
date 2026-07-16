# Implementation plan — M1 to M5, to feature-complete v1

The detailed work plan behind [`V1_PLAN.md`](V1_PLAN.md). Each milestone lists its tasks in build order with
**where** (files), **how** (approach), and **done when** (verifiable acceptance criteria). Backlog P-numbers
map 1:1. Work strictly in milestone order — every milestone ends in a shippable, verified state.

Feature-complete v1 = everything in this file done = the bot answers on the Cratis Discord server per
[`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md), deployed per [`DEPLOYMENT.md`](DEPLOYMENT.md), with
measured answer quality gating changes.

> Verification discipline for every task: `dotnet build -c Release` (zero warnings) + `dotnet test` green +
> the milestone's own end-to-end check. Specs for pure logic follow the Cratis.Specifications style already
> in `Specs/`.

> Status (2026-07-16): M1–M5 are code-complete and spec-verified (256 specs). The ✅ markers below flag
> shipped tasks; where a task's own "done when" is a *live* run, the marker names the remaining key/token/deploy
> gate. M2.2–M2.4 (calibration, optional query rewrite, prompt caching) and M5.3–M5.5 (deploy, privacy notice,
> observability) are not yet done.

## M1 — Ingestion (real index runs)

Prerequisite: a Voyage API key in `Cratis__Prompter__Voyage__ApiKey` (free tier covers everything).

1. **Batch embeddings (P-02)** ✅ **Done** — `Source/Ingestion/Indexer.cs`
   Collect changed chunks per page (or across pages) into batches of up to **128 inputs** per
   `GenerateAsync` call (Voyage API limit; also verify the per-request token cap in Voyage docs before
   picking batch size), upsert per batch. Add retry with exponential backoff on 429/5xx around the embedding
   call (`Polly` is unnecessary — a small helper in `Embeddings/` is fine).
   *Done when:* full index of cratis.io (~870 pages) completes in one `dotnet run -- index` on a fresh
   database, and a second run reports `0 embedded` / all unchanged. **Batching + `ResilientEmbeddingGenerator`
   retry shipped and spec-covered; this live full-corpus run is pending a Voyage API key.**

2. **Configurable exclusions (P-03)** ✅ **Done** — `Source/PrompterOptions.cs` (`IngestionOptions` with
   `ExcludedPathSegments`, defaults `["client-snippets", "api-reference"]`), consumed by `DocsSite`.
   `DocsSite.ParsePageUrls` becomes instance-based or takes the list as a parameter (keep it pure/static for
   specs — parameter preferred).
   *Done when:* specs cover custom exclusions; defaults unchanged. **Met — spec-covered.**

3. **MDX component noise (P-01 residue)** ✅ **Done** — `Source/Ingestion/MarkdownChunker.cs`
   Strip JSX-ish component blocks that survive in `.md` mirrors (`<CardGrid>…</CardGrid>`, `<TopicHero …>`,
   self-closing `<SimpleCard … />`) outside code fences. Keep inner text where it is prose (e.g. hero text).
   Inspect 3–5 real mirrors first (`curl https://cratis.io/index.md`, `/arc.md`, `/chronicle/getting-started.md`)
   and write the specs from real fixtures.
   *Done when:* chunks from the real landing page contain no `<`-component tags; specs prove it. **Met —
   `StripMdxComponents` spec-covered against real `index.md`/`arc.md` mirror fixtures.**

4. **Index-run summary as data** ✅ **Done** — extend `IIndexer.Run` to return a small `IndexRun` record
   (pages, embedded, unchanged, removed, duration) and print it in `index` mode; the webhook (M5) and any
   future scheduled run reuse it.
   *Done when:* `dotnet run -- index` prints the summary line. **Shipped (`IndexRun` + the summary line in
   `Program.cs`); the printed line appears on a real index run.**

## M2 — Retrieval + Answering (quality of the core loop)

Prerequisite: M1 indexed corpus + an Anthropic API key.

1. **`ask` UX (P-10)** ✅ **Done** — `Source/Program.cs`: `--verbose` flag printing per-passage score/page/heading before
   the answer; exit non-zero on refusal (useful in scripts/CI probes).
   *Done when:* `dotnet run -- ask "How do I append an event in Chronicle?" --verbose` shows passages and a
   cited answer against the real corpus. **`AskArguments`/`AskOutput` parse/render/exit logic spec-covered;
   this live run is pending Voyage + Anthropic keys.**

2. **Threshold calibration (P-07)** — collect ~20 real questions (Discord history, FAQ) + ~5 out-of-scope
   ones ("what's the best pizza"), run them through `ask`, record top scores, set `Answering:MinScore` so all
   out-of-scope refuse and in-scope pass. Record findings in this file under a "Calibration" note.
   *Done when:* the 25 probes behave correctly with the committed default.

3. **Query rewriting (P-08, optional — skip if probes look good)** — small pre-retrieval step turning
   conversational phrasing into a search-friendly query using Haiku via a second, cheap `IChatClient`
   (keyed DI service). Only add if M2.2 shows retrieval misses that rewriting fixes.

4. **Prompt caching (P-09)** — mark the system prompt cacheable once the Anthropic SDK exposes cache-control
   through `Microsoft.Extensions.AI` options (check SDK release notes; if not exposed yet, note it and move on
   — at current volumes this is cents).

## M3 — Discord (the full integration set)

The behavior contract, intents, permissions, and app-setup runbook live in
[`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md) — implement against that spec. Build order:

1. **Deferred responses for `/ask` (P-11)** ✅ **Done** — `Source/Discord/Ask.cs`
   Answering takes 5–15 s; interactions must ack within 3 s. Respond with
   `InteractionCallback.DeferredMessage()` immediately, then send the answer as a followup. Verify the exact
   NetCord 1.0.0-beta.11 API surface (`Context.Interaction.SendResponseAsync` / followup method) against
   https://netcord.dev docs — this was not compile-verified in the scaffold.
   *Done when:* `/ask` on a test server shows "thinking…" then the cited answer, including for slow answers.
   **Deferred ack + followup shipped (NetCord beta.11 API confirmed); this "thinking… → answer" runtime check
   is pending a Discord token + test server (and keys).**

2. **Mention hardening (P-12)** ✅ **Done** — `Source/Discord/Mentions.cs`
   Handle nickname mentions (`<@!id>`) and ignore role mentions; verify `GatewayClient.Id` is populated for
   self-identification (scaffold assumption) — if not, resolve the application id at startup via REST.
   Reply in-channel as a reply-reference to the asking message.
   *Done when:* mention specs cover both mention forms; manual test answers on the test server. **Specs met
   (both mention forms; `GatewayClient.Id` confirmed from READY); the manual test-server check is token-gated.**

3. **Dedicated ask channel** ✅ **Done** — new handler behavior in `Mentions` (rename to `Questions` if cleaner):
   messages in `Discord:AskChannelId` are treated as questions without requiring a mention (the Prisma
   `#ask-ai` pattern). Add `AskChannelId` to `DiscordOptions`.
   *Done when:* plain messages in the configured channel get answered; other channels still require mention.
   **Shipped in `Mentions.ResolveQuestion` (`AskChannelId` on `DiscordOptions`, kept in `Mentions`);
   the live check is token-gated.**

4. **Forum auto-reply (P-13)** ✅ **Done** — new `Source/Discord/HelpForum.cs` implementing NetCord's guild-thread-create
   gateway handler interface: when a thread is created under `Discord:HelpForumChannelId`, fetch the starter
   message, answer as the first reply, and add a standing line ("A human will follow up — reactions below tell
   us if this helped.").
   *Done when:* creating a forum post on the test server yields an automatic cited answer in-thread. **`HelpForum`
   shipped (`ShouldAnswer` spec-covered; standing note now points at the 👍/👎 buttons); this runtime check is
   token-gated.**

5. **Rate limiting (P-14)** ✅ **Done** — small in-memory token bucket per user hash (bot is single-instance), default
   5 questions / 10 minutes, friendly refusal message when exceeded. Config under `DiscordOptions`.
   *Done when:* spec for the bucket logic; 6th rapid question on the test server gets the friendly limit reply.
   **`RateLimiter` bucket spec-covered and wired at every entry point (mention, `#ask`, `/ask`, forum), with a
   fail-fast `WindowMinutes > 0` startup validation; the live 6th-question check is token-gated.**

6. **Long answers (P-15)** ✅ **Done** — `Source/Discord/DiscordAnswers.cs`: instead of truncating at 2000 chars, split on
   paragraph boundaries into successive messages (max 3), keeping sources on the last message.
   *Done when:* spec proves splitting; long real answer arrives complete on the test server. **`DiscordAnswers.Split`
   spec-covered (incl. code-fence safety); the live check is token-gated.**

7. **Feedback reactions (P-16)** ✅ **Done (shipped as 👍/👎 buttons, not reactions — see BACKLOG P-16)** — bot adds 👍/👎 to its own answers; a message-reaction-add gateway handler
   records the verdict onto the interaction row. Requires `interactions.answer_message_id` column — first
   schema change, so do P-04 (versioned SQL migration files, Ada-style `Storage/Migrations/v1_1_0.sql` applied
   in `EnsureSchema`) as part of this task.
   *Done when:* reacting on the test server flips the row's feedback column; migration applies cleanly to an
   existing database. **Shipped as 👍/👎 **buttons** (per `DISCORD_BEST_PRACTICES.md`), not reactions:
   `v1_1_0.sql` adds `answer_message_id` + `feedback`, and a `Feedback` component handler writes the verdict;
   `FeedbackButton`/`FeedbackVerdicts` spec-covered. The live click-flips-the-row check is token-gated.**

8. **Resilience** ✅ **Done** — wrap answer generation per question with a timeout (60 s) and a catch-all that logs and
   posts a short apology instead of going silent; the gateway handler must never throw.
   *Done when:* forced failure (bad API key) produces the apology message, not silence. **Shipped: a 60 s
   `AnswerTimeoutSeconds` cancellation and a per-handler catch-all that posts `DiscordOptions.ErrorReply`; the
   forced-failure check is token/key-gated.**

## M4 — Evaluation (the quality gate)

1. **Golden set (P-17)** ✅ **Done** — `Eval/golden-questions.yaml` (new folder): ≥40 in-scope questions with expected
   grounding pages + ≥10 out-of-scope questions expected to refuse. Source from Discord history and the FAQ.
   **Shipped: 57 in-scope questions across all five products + 12 out-of-scope refusals (incl. adversarial
   near-misses).**
2. **Harness (P-18)** ✅ **Done** — new `Eval/Prompter.Eval.csproj` console project (referenced in the slnx, excluded from
   Docker publish): runs the golden set through `IAnswers`, scores groundedness/citation-hit/refusal-accuracy
   with `Microsoft.Extensions.AI.Evaluation` (crib `dotnet/eShopSupport`'s `AnswerScoringEvaluator`), writes a
   markdown + JSON report to `Eval/results/`.
   *Done when:* `dotnet run --project Eval` produces a scored report against the live corpus. **Harness shipped
   (pure scorers spec-covered); the scored report is pending Voyage + Anthropic keys + an indexed corpus.**
3. **CI gate (P-19)** ✅ **Done** — `.github/workflows/eval.yml` on `workflow_dispatch` + PRs labeled `eval` (API keys via
   repo secrets; not on every PR — cost control). Fails if scores drop below the committed baseline in
   `Eval/baseline.json`.
   *Done when:* a deliberately broken prompt fails the workflow; the good prompt passes. **Workflow +
   `Eval/check-baseline.py` shipped; it becomes a live gate once `Eval/baseline.json` is regenerated from a
   real run with keys.**

## M5 — Operations (deploy + keep fresh)

Runbook detail in [`DEPLOYMENT.md`](DEPLOYMENT.md). Build order:

1. **Re-index webhook (P-20)** ✅ **Done** — convert `Program.cs` bot mode to `WebApplication` (Kestrel) hosting the
   gateway client *and* two endpoints: `GET /healthz` (checks DB + gateway connected) and `POST /reindex`
   (constant-time shared-secret header check → runs `IIndexer.Run` in the background, 409 if already running).
   Wire the trigger: the `Cratis/Documentation` repo's docs-site workflow gains a step calling the endpoint
   (or a tiny `prompter-reindex.yml` workflow in this repo on `repository_dispatch` calls it) — decide with
   the team which side owns the call.
   *Done when:* `curl -X POST -H "X-Reindex-Secret: …" host/reindex` re-indexes; wrong secret → 401.
   **Endpoints shipped and runtime-smoke-tested (`ReindexAuth`/`ReindexGate` spec-covered); wiring the
   Documentation build to call `/reindex` (+ ingress + k8s secret) is deploy-gated (M5.3).**
2. **Retention job (P-22)** ✅ **Done** — `IHostedService` running `IInteractionLog.PurgeExpired()` daily.
   *Done when:* rows older than `RetentionDays` disappear on schedule (test with a short window). **Shipped as
   `RetentionPurge` (cadence + resilience spec-covered; the `DELETE` cutoff was live-verified against a
   throwaway Postgres).**
3. **Deploy (P-21, P-26)** — per [`DEPLOYMENT.md`](DEPLOYMENT.md): GitHub repo + secrets, first `publish.yml`
   release, then a Studio-style `deploy-production.yml` pins the image tag and runs `pulumi up` against the
   UpCloud UKS cluster (D-11) — bot Deployment + in-cluster Postgres/pgvector — and the bot joins the real
   server with channels configured.
   *Done when:* the bot answers on the real Cratis Discord and survives a pod reschedule (single-replica k8s
   Deployment, restarted by the cluster).
4. **Privacy notice (P-23)** — pinned message in the server + the `Documentation/index.md` privacy section
   kept in sync; register Prompter's docs in the Documentation repo (P-24).
5. **Observability** — structured logs to stdout (docker logs), the `/healthz` endpoint watched by a free
   uptime monitor, and a weekly look at `interactions` (refusal rate, feedback ratio) until the eval digest
   exists.

## Explicitly out of v1 (parking lot in BACKLOG)

Confidence-gated chime-in · docs-MCP server · weekly unanswered-questions digest · reranking experiment ·
Chronicle dogfooding (D-6, needs ruling).
