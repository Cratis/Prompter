# Decision records

Durable rulings for Prompter. Do not re-litigate — append new decisions with the next D-number and a date.
Open decisions are marked **OPEN** and carry a recommendation.

## D-1 · Name: Prompter — 2026-07-15

In theater, the prompter sits just offstage with the script and feeds the line to anyone who forgets. The docs
are the script; the bot whispers the answer. Fits the storytelling family (Chronicle, Narrator, Lens, Arc,
Studio) and carries the LLM-prompt double meaning. GitHub `Cratis/Prompter` and Docker Hub `cratis/prompter`
were free at decision time.

## D-2 · Build, don't buy — 2026-07-15

Custom bot over kapa.ai/Inkeep/CrawlChat/Onyx. Grounds: Discord is the only surface we need (kapa's thinnest
integration); kapa's paid tier is ~$12–25k/yr with a discretionary OSS-program exit clause; the vendor category
consolidated hard through 2025–26 (Mendable dead, Threado dissolved, RunLLM pivoted); kapa hosts US-only with
enterprise-gated DPA — friction for a Norway-based controller; and our docs pipeline already emits LLM-ready
artifacts, making the build ~9–12 developer-days at ≤ €15/month run cost. Full evidence:
[`RESEARCH.md`](RESEARCH.md). Fallback if we ever reverse: Inkeep (BYO-LLM keys, zero-retention options) over
kapa for GDPR reasons.

## D-3 · Discord library: NetCord — 2026-07-15

NetCord (net10.0-only, Generic Host/DI-native, most active development) over Discord.Net and DSharpPlus. It
matches how we build hosts everywhere else. Accepted risk: pre-1.0 churn. Recorded fallback: Discord.Net 3.20.x
if beta breakage costs more than it saves — the Discord feature folder is the only thing that would change.

## D-4 · Vector store: Postgres + pgvector, hybrid search in SQL — 2026-07-15

One Postgres on the same box, `Pgvector` + Npgsql, exact scan (no ANN index needed at ~20k chunks), and hybrid
retrieval (tsvector BM25 + cosine, RRF-fused) as one SQL query. No dedicated vector database, no
Microsoft.Extensions.VectorData connector for search (its Postgres connector lacks hybrid support — we write
the SQL). Anthropic's contextual-retrieval benchmarks show hybrid roughly halves top-20 retrieval failure —
this is the one quality technique we commit to from day one.

## D-5 · Models: Claude for answers, Voyage for embeddings — 2026-07-15

Answer generation: Claude Sonnet via the official `Anthropic` NuGet SDK exposed as
`Microsoft.Extensions.AI.IChatClient`, prompt-cached system prompt. Embeddings: Voyage `voyage-4` behind our
own `IEmbeddingGenerator` (Anthropic has no embeddings endpoint; Voyage's free tier covers our corpus
hundreds of times over). Both sit behind Microsoft.Extensions.AI abstractions — swapping models is config, not
code. Model choice here is a quality decision, not a cost one (whole-community LLM spend is $1–10/month).

## D-6 · Chronicle dogfooding for the interaction log — OPEN

The Q&A interaction log (question asked, answer given, feedback received) is naturally event-shaped, and a bot
built on our own platform is a good story. But Chronicle + MongoDB on the same small box doubles the moving
parts before the bot has proven itself. **Recommendation:** v1 logs interactions through the `IInteractionLog`
seam into Postgres; revisit after M5 — adopting Chronicle then is an adapter swap, not a redesign. Needs a team
ruling before anyone builds analytics on the Postgres shape.

## D-7 · Interaction model: mention + forum auto-reply + #ask — no auto-chime in v1 — 2026-07-15

@mention anywhere, `/ask` slash command, auto-reply to new threads in the designated help forum channel, and a
dedicated #ask channel. No unprompted interjections in text channels: every surviving vendor converged on this
model, and unsolicited bot answers erode trust faster than they help. A confidence-gated chime-in on explicitly
opted-in channels is a post-v1 experiment (parking lot), enabled per channel, threaded, and easy to mute.

## D-8 · GDPR posture — 2026-07-15

We are the controller (Norway/EEA; Datatilsynet). v1 rules: store Discord user IDs **hashed** (rate limiting
and abuse control need identity-shaped data, analytics does not need identities); question/answer text retained
on a configurable window (default 90 days) with an automatic purge job; a pinned privacy notice in the server
naming the bot, what it processes, and the LLM subprocessor; DSAR path is "delete by hashed ID". Model traffic
goes to the Anthropic API (no training on API data by default); if EU-region inference becomes a requirement,
Claude via Vertex AI (europe-west) or Bedrock (eu-central-1) is a config change behind `IChatClient`.

## D-9 · Ingestion source: the published site, not the repos — 2026-07-15

Ingest the per-page Markdown mirrors and `llms.txt` index that the Documentation site already generates on
every deploy (aggregated, DocFX-isms stripped, links resolved) instead of crawling ~15 product repos and
re-implementing `sync-content.mjs`. Exclude Chronicle's generated `client-snippets/` fragments. Freshness rides
the existing `build-docs` `repository_dispatch` — the Documentation repo already tells the world when docs
change; Prompter subscribes via a protected webhook (M5).

## D-10 · Specs discipline — 2026-07-15

Cratis.Specifications BDD style throughout (`for_<Type>/when_<behavior>/…`), same as every other repo. Pure
logic (chunking, hashing, RRF math, prompt assembly, refusal thresholds) gets specs from day one; the golden
Q&A eval harness (M4) is the spec suite for answer quality — a failing groundedness score blocks merge the same
way a failing spec does.

## D-11 · Deploy on the existing UpCloud cluster, Studio-style — 2026-07-15

Prompter deploys to the **UpCloud UKS cluster (region `no-svg1`, Stavanger/Norway) that Studio's Pulumi stack
manages**, following Studio's conventions (Pulumi C#, self-managed in-repo state, passphrase secrets,
`UPCLOUD_TOKEN`, version-pinned images via `pulumi config set`, deploy workflow called from Publish). This
supersedes the original standalone-Hetzner topology: no new infrastructure to operate, observability
(Loki/Grafana) and backup patterns already exist, and the Norway region strengthens D-8. Marginal run cost
drops to the LLM API alone (~$1–10/mo) — the cluster, storage, and backups are already paid for — which
supersedes the ≤€15/mo standalone-box figure cited in D-2. Postgres+pgvector runs in-cluster with
object-storage backups, mirroring the cluster's MongoDB precedent (managed Postgres is the fallback if
pgvector support checks out and in-cluster proves annoying). **Open sub-question Q-5:** whether the Pulumi
code lives as a workload entry in Studio's `Deployment/` stack (recommended — the established pattern for
platform services like `studio-llm` and Prologue) or as a `Deployment/` project in this repo. See
[`DEPLOYMENT.md`](DEPLOYMENT.md).

## D-12 · License and visibility: open source (MIT, public) — OPEN

**Recommendation: open source, MIT, public from day one.** Grounds: (a) the whole Cratis org is MIT/public —
a closed bot answering questions about an open platform from public docs would be off-brand, and community
trust in a devtools Discord depends on it; (b) there is nothing to protect — the code is a thin RAG pattern
over public content, the valuable assets (docs, community, interaction data, keys) are not in the code, and
the 2025–26 vendor consolidation shows "bot as a paid product" is not a business worth preserving optionality
for; (c) transparency is a GDPR asset — the privacy notice can link to the source proving hashed IDs and the
retention purge (strengthens D-8); (d) it is a showcase for the Cratis AI-native story (.NET 10 +
Microsoft.Extensions.AI + Claude + pgvector with a measured eval gate), and a configurable-docs-site stretch
makes it adoptable by any Starlight project. Accepted trade-off: the system prompt and refusal thresholds are
readable — obscurity is weak protection anyway, and blast radius is capped by minimal permissions + rate
limits. Guardrails when public: secrets stay in env/Pulumi-encrypted config only (already true), deployment
state lives with the stack (Q-5), and the README sets expectations (built for the Cratis community, PRs
welcome, no support promises). Needs a team ruling before `Cratis/Prompter` is created (P-26) — visibility is
easiest to set correctly at creation time.
