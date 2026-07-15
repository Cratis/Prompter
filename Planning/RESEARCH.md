# Research — buy vs build for a docs-grounded Discord bot (2026-07-15)

Condensed record of the multi-agent research (five verified tracks, primary sources fetched live 2026-07-15)
that led to building Prompter. This is background material — decisions extracted from it live in
[`DECISIONS.md`](DECISIONS.md).

## Market

- **kapa.ai** is the category leader (Astro, Prisma, Nuxt run it in their Discords). OSS program: up to 10,000
  free questions/month but requires "non-commercial", with a one-sentence discretionary transition clause when
  a project commercializes. Paid: median contract **$25,200/yr** (Vendr; range $12k–83k), no startup tier.
  Vendor: 22 people, $3.7M seed, no Series A as of July 2026. Discord bot is mention-only in text channels +
  forum auto-reply — its thinnest integration. No llms.txt support, no CI-triggered re-index (daily crawl), no
  model choice, no self-hosting. <https://docs.kapa.ai/kapa-for-open-source>, <https://www.vendr.com/marketplace/kapa-ai>
- **The category consolidated brutally in 2025–26:** Mendable shut down (~$250k ARR, pivoted to Firecrawl,
  site never announced it); Threado dissolved March 2025; RunLLM pivoted to AI-SRE ("Herald") and its Discord
  docs-bot product no longer exists; Discord killed its own Clyde AI.
- **Inkeep** is the only funded head-to-head competitor still selling a Discord docs bot ($13M, Sept 2025;
  pivoting upmarket to agent platforms). No OSS free tier; ~$150–500/mo per stale third-party figures.
  BYO-LLM keys and zero-retention options — the better GDPR story among hosted vendors.
- **Budget tier:** CrawlChat ($29–99/mo, real Discord+GitHub+MCP support, unfunded indie); Wallu, Mava
  (thin, non-technical focus). DocsBot/Chatbase/CustomGPT have **no native Discord bots**.
- **Self-hosted:** Onyx (ex-Danswer, MIT CE, ~31k stars) is the only serious option and uniquely offers
  per-channel "answer all messages" — but it means operating a full enterprise-search platform for one bot.
  RAGFlow's Discord support was ~1 month old and needs a 16 GB box. Standalone GitHub "Discord RAG bots" are
  dead or unlicensed.
- **Communities pattern-match to two camps:** pay kapa (Astro — who archived their own bot "Houston" in Feb
  2025 and bought; Prisma; Nuxt) or build in-house (Effect-TS, Supabase). tRPC/Nuxt additionally run
  **Answer Overflow** (indexes solved Discord threads into Google) — complementary, worth adding regardless.

## GDPR (we are Norway-based; the controller is us)

- kapa: US company (NY law), **US-only hosting on Google Cloud**, no EU residency, DPA "for enterprise
  customers", no public subprocessor list, PII masking **off by default**, conversations stored with end-user
  ID/email fields. Workable but paperwork-heavy: Art. 28 DPA, member privacy notice, legitimate-interest
  assessment, transfer mechanism (their DPF claim was not independently verifiable).
- Self-hosting collapses the problem: conversation logs stay on our infra; one subprocessor (LLM API);
  Anthropic doesn't train on API data by default; EU-region inference available via Vertex AI (europe-west) or
  Bedrock (eu-central-1) if required.
- Our operator obligations regardless of vendor: privacy notice in the server, lawful basis (legitimate
  interest + minimization), deletion path. Implemented as D-8.

## Build cost & stack (verified July 2026)

- **Effort:** ~9–12 developer-days to production-quality v1 (MVP 3–4 days; the tail is the eval harness —
  where quality is actually won).
- **Run cost:** Hetzner CAX11 ARM ~€6.50/mo (bot + Postgres in Compose) + $1–10/mo LLM API (50–200 q/mo,
  prompt-cached) + ~$0 embeddings (Voyage free under 200M tokens; corpus is ~2M). Claude pricing at decision
  time: Haiku 4.5 $1/$5 per MTok, Sonnet 5 $2/$10 intro through 2026-08-31 then $3/$15.
  *(Superseded by D-11: deploy moved to the shared UpCloud UKS cluster — no dedicated box, so the ~€6.50/mo
  Hetzner line falls away and the marginal cost is the LLM API alone.)*
- **Stack:** NetCord (or Discord.Net fallback) · Microsoft.Extensions.AI (GA) · official `Anthropic` C# SDK as
  `IChatClient` · Postgres + pgvector 0.8 (exact scan at ~20k chunks) · hybrid BM25+vector RRF in one SQL query
  (the Supabase/Katz pattern) · Voyage `voyage-4` embeddings behind `IEmbeddingGenerator`.
- **Quality technique with verified evidence:** contextual embeddings + contextual BM25 roughly halved top-20
  retrieval failure (5.7%→2.9%) in Anthropic's benchmarks
  (<https://www.anthropic.com/engineering/contextual-retrieval>). The often-quoted "+reranker −67%" figure did
  **not** survive adversarial verification — treat reranking as an experiment, not a given.
- **Reference implementations to crib from:** `dotnet/eShopSupport` (golden-Q&A eval harness,
  `Microsoft.Extensions.AI.Evaluation`), the `dotnet new aichatweb` template (RAG ingestion + citation
  tracking), Supabase's pgvector docs-search templates, `ragpi` (architecture reference).

## Our unfair advantages

- The Documentation site already emits **`llms.txt`/`llms-full.txt` and a per-page `.md` mirror on every
  deploy** (Starlight + `starlight-llms-txt`) — ingestion needs no crawler and no DocFX conversion.
- Product repos already dispatch **`build-docs`** to the Documentation repo on every doc merge — the freshness
  trigger exists; Prompter just subscribes.
- Corpus is clean and single-version: ~738 prose pages (~2M tokens), Diátaxis-structured, zero-broken-link CI.
  One trap: exclude Chronicle's 697 generated `client-snippets/` fragments.
- The `AI/` repo's rules and glossary are ready-made grounding/prompt material, and `Chronicle.Mcp` already
  ships on the same `Microsoft.Extensions.AI` plumbing this bot uses.
