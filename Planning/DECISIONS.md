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

## D-6 · Chronicle dogfooding for the interaction log — decided: NOT now — 2026-07-16

The Q&A interaction log (question asked, answer given, feedback received) is naturally event-shaped, and a bot
built on our own platform is a good story. It was actively reconsidered on 2026-07-16 and **ruled out for the
foreseeable roadmap** — "it's just a Discord bot." A four-repo research pass (Chronicle/Arc/Ada) established
that Chronicle is a **separate gRPC kernel server** (not an in-process library — it can't share our Postgres
in-process), that its Postgres backend is only ~7 weeks old (Mongo is the proven path), and that adopting it
would add a hard runtime dependency + a local kernel container to an otherwise self-contained bot — for
analytics value we don't need at launch. Our GDPR posture (D-8) already covers v1 without it. v1 keeps the
simple Postgres `IInteractionLog`. Revisit only on a concrete pull, from the findings and build path captured
in [`CHRONICLE_RESEARCH.md`](CHRONICLE_RESEARCH.md) — adopting Chronicle then is still an adapter swap behind the
`IInteractionLog` seam, not a redesign.

## D-7 · Interaction model: mention + forum auto-reply + #ask — no auto-chime in v1 — 2026-07-15

@mention anywhere, `/ask` slash command, auto-reply to new threads in the designated help forum channel, and a
dedicated #ask channel. No unprompted interjections in text channels: every surviving vendor converged on this
model, and unsolicited bot answers erode trust faster than they help. A confidence-gated chime-in on explicitly
opted-in channels is a post-v1 experiment (parking lot), enabled per channel, threaded, and easy to mute.

## D-8 · GDPR posture — 2026-07-15

> **Amended by [D-13](#d-13--interaction-log-stores-no-personal-data--2026-07-16) (2026-07-16):** the
> interaction log now stores **no personal data at all** — no message content and no user identifier (not
> even a hash). The hashed-ID, question/answer-text retention, and DSAR-by-hash rules below are superseded by
> data minimization; the rest of D-8 (controller status, subprocessor posture, EU-inference fallback) stands.

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

## D-12 · License and visibility: open source (MIT, public) — 2026-07-15

**Decided 2026-07-15** (ruled by the team when creating the repo): open source, MIT, public from day one —
`https://github.com/Cratis/Prompter` was created public the same day. Original rationale: Grounds: (a) the whole Cratis org is MIT/public —
a closed bot answering questions about an open platform from public docs would be off-brand, and community
trust in a devtools Discord depends on it; (b) there is nothing to protect — the code is a thin RAG pattern
over public content, the valuable assets (docs, community, interaction data, keys) are not in the code, and
the 2025–26 vendor consolidation shows "bot as a paid product" is not a business worth preserving optionality
for; (c) transparency is a GDPR asset — the privacy notice can link to the source proving hashed IDs and the
retention purge (strengthens D-8); (d) it is a showcase for the Cratis AI-native story (.NET 10 +
Microsoft.Extensions.AI + Claude + pgvector with a measured eval gate), and a configurable-docs-site stretch
makes it adoptable by any Starlight project. Accepted trade-off: the system prompt and refusal thresholds are
readable — obscurity is weak protection anyway, and blast radius is capped by minimal permissions + rate
limits. Guardrails now that it is public: secrets stay in env/Pulumi-encrypted config only (already true),
deployment state lives with the stack (Q-5), and the README should set expectations (built for the Cratis
community, PRs welcome, no support promises — still to add).

## D-13 · Interaction log stores no personal data — 2026-07-16

**Amends [D-8](#d-8--gdpr-posture--2026-07-15).** The interaction log keeps **no personal data**: no message
content (question/answer text) and no user identifier — not even the keyed hash D-8 called for. A `v1_2_0`
migration drops the `question`, `answer`, `user_hash`, and `answer_message_id` columns, leaving only anonymous
operational signal per answer: `source`, `cited_pages`, `confidence`, `was_refusal`, and `feedback`. Raw
Discord user IDs are also removed from the operational logs. Rate limiting still tells users apart, but only
via an **in-memory** key (the raw id) that is never written to disk or logs — so the whole `UserHash`/keyed-hash
apparatus and its required key are gone.

Grounds: the log was **write-only** — no running code read the content back; its value was entirely
prospective (analytics + the post-v1 docs-gap flywheel). Data minimization is a GDPR principle, and storing
nothing identifiable takes the log **out of scope entirely** — no lawful-basis, retention, or DSAR obligations
attach to anonymous rows, and the public privacy notice can make the strongest possible claim ("we keep no
message content and nothing that identifies you"), verifiable in the open source. The retention purge stays as
housekeeping (bounding table growth), not as a privacy control. Accepted trade-off: the docs-gap flywheel
([BACKLOG](../BACKLOG.md) P-33) will need question text, so it must **re-introduce content behind its own
decision record** (consent notice + narrow retention) rather than assuming it is already collected. The
`IInteractionLog` seam is unchanged, so that is an additive change, not a redesign.
