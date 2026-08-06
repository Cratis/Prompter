# Session handover

Resume state for anyone (human or agent) continuing work in a fresh session. Newest entry first — append,
don't rewrite history.

## 2026-08-06 (end of day) — Stage 0 split out for the org owner

**[`ORG_SETUP.md`](ORG_SETUP.md) + [issue #10](https://github.com/Cratis/Prompter/issues/10)** carve out what
belongs to whoever owns the Cratis accounts — the Voyage and Anthropic keys and the Discord application —
from what anyone can then do on a laptop. Stage A (#6) is marked blocked on it, and the playbook's A1/A4 now
defer to it rather than duplicating the steps.

The reason it is split is durability, not process: a model-provider key on a personal card and a Discord app
owned by an individual account are both single points of failure — the day that account is unavailable the
bot cannot be recovered and its token cannot be rotated by anyone else. Hence the two rules the document
leads with: **keys on the org account**, and **the Discord application inside a Discord Team** that can have
several owners. The Anthropic step also suggests a capped Workspace, so a misconfiguration costs pennies
rather than the org's budget.

Everything the owner needs later — `PULUMI_CONFIG_PASSPHRASE`, `UPCLOUD_TOKEN`, the runner check, DNS, the
issue-filing PAT — is in the same document in a table against the stage that needs it, so none of it has to
be decided now.

## 2026-08-06 (end of day) — Readiness recorded: proven vs. never-run, and the gates

**v0.2.0 released** (`cratis/prompter:0.2.0` + `:latest`, amd64/arm64) and the published image was pulled and
started — it comes up and stops at the `Discord:Token` validation, which is the correct behavior with no
configuration.

**Three things verified against reality rather than assumed**, all in the outbound GitHub path: the exact
issue-filing payload works; **GitHub creates a label that does not exist yet** (`from-discord`) on first use,
so no repository needs preparing — that was a claim written into a code comment and is now checked; and the
duplicate-search response shape matches what `Issues.FindSimilar` parses.

**The playbook now opens with a readiness section** naming what is proven and what has **never executed** —
the corpus has never been indexed, no Discord surface has touched a live gateway, `/issue`'s Discord half has
not run, `MinScore` is still the committed guess, `pulumi up` has never been applied, the deploy workflow has
never run, and `Eval/baseline.json` is a placeholder. The gap is entirely things not yet *done*.

**The gates are written down, in order:** Stage A in full including the A6 calibration → Stage B → **a week of
answering only** on the real server → Stage C, with C1 any time, C2 next, and **C3 last on a single
repository** because answering is the only step that writes into somebody else's tracker. The playbook also
ranks what is most likely to bite first (the threshold; slash-command registration and the `/issue`
interaction flow; the first `pulumi up`; then answering on a real tracker).

**Housekeeping:** verifying the filing path created issue **#9**. It is closed as not planned, retitled, and
its labels removed, so it cannot appear when anyone filters for `from-discord` or `bug` — the filters the
feature itself depends on — and the duplicate search only looks at open issues, so it will never be offered
as a match either. It is inert where it stands. Deleting it outright needs the **web UI** (Issue → ⋯ →
Delete issue): GitHub refuses issue deletion for CLI/OAuth tokens regardless of admin rights, which is worth
knowing before anyone spends time on it.

The probe also left the `from-discord` label behind, which is the one Prompter puts on everything it files —
now given a description and Discord's blurple so it reads as deliberate rather than auto-generated. Labels
that do not exist are created on use, so this was never a prerequisite; it just looks better this way.

## 2026-08-06 (evening) — The tracker bridge is built: P-44, P-45 and P-46 ship

**State:** Release build **0 warnings**, **389 specs green** (up from 278 — 111 new). Everything D-16
described now exists in code, off by default until its credentials are set, so an existing deployment is
unaffected.

**What shipped.**

- **P-45 · `/issue`** — describe something in Discord, Prompter drafts the issue (title, body, kind, product)
  with the model, routes it to the owning product repository, checks for likely duplicates, and shows an
  **ephemeral** preview with Create/Cancel. Nothing reaches GitHub until the button is pressed. Drafts live in
  memory for 15 minutes and are *taken* on click, so a double-click cannot file twice and an abandoned draft
  leaves no trace — filing stays consent-in-the-moment and D-13 is untouched. Issues carry a `from-discord`
  label, a kind label, and a link back to the thread; no Discord username is written into a public tracker.
- **P-44 · `POST /github/webhook`** — verifies GitHub's `X-Hub-Signature-256` HMAC against the raw body,
  ignores everything that is not `issues.opened` (so a repository can point its whole webhook at it), skips
  bots and pull requests, honors a `no-prompter` label, and answers only for allowlisted repositories.
  **Silence on refusal** is the rule: an ungrounded answer is never posted.
- **P-46 · maintainer announcement** — the enriched half only: the message says whether the docs already
  answer the issue, which is what turns a notification into triage. The plain notification is still better
  served by GitHub's own Discord webhook, and the playbook says so.
- Stack, ingress and secrets updated; `Documentation/guides/reporting-issues.md` written for the community;
  `/issue` added to the Discord behavior contract; playbook gained **Stage C** to switch it all on.

**A spec caught a real bug:** draft eviction ordered by timestamp, which ties when several drafts are held in
the same clock tick, so "drop the oldest" dropped an arbitrary one. Now ordered by insertion sequence.

**Not built, deliberately.** **P-47** (handing mechanical issues to a coding agent) — Prompter files issues;
GitHub's own agents act on them, behind a human-applied label, draft PRs only, opt-in repos. It needs its own
decision record before any repository opts in. Also outstanding: the message context-menu entry point ("File
as issue" on an existing message) needs NetCord's message-command context, which was not compile-verifiable
against beta.12 in this pass; the slash command covers the same ground meanwhile.

**On PAT vs GitHub App:** the token is one secret and no setup, so the playbook starts there — but it acts as
the person who created it (issues show *their* name), expires, and shares that person's rate limit. An App
gives Prompter its own identity and no expiry for about an hour of setup. The code sends either as a bearer
token, so switching is changing one secret.

## 2026-08-06 (later still) — Go-live playbook written; the tracker-bridge scope corrected (D-16)

**A misread worth recording:** the "file issues from Discord" idea was scoped here as a *docs-gap* feature
(P-33's flywheel). That was too narrow — the intent is bugs, missing APIs, feature requests, ideas **and**
docs gaps, plus auto-implementing the mechanical ones. [D-16](DECISIONS.md) records the corrected scope:
Prompter moves work between the community and the trackers in both directions, and answering is only half
its job. P-45 is rewritten accordingly (a `/issue` command and a message context-menu action, Prompter drafts
the issue, an ephemeral preview confirms, then it files), **P-46** added (Discord notification when an issue
opens — the zero-code GitHub→Discord webhook first), and **P-47** added (mechanical issues handed to
*GitHub's* coding agents behind a human-applied label, draft PRs only; Prompter never runs an agent).

**Q-7 answered:** issues go to the **owning product repo**, routed. That promotes P-30's product classifier
from nice-to-have to a dependency of P-45. **Filing is open to anyone** — at current community volume an
approval step would cost more in missed reports than it saves in noise; duplicate detection, a per-user cap
and a `from-discord` label carry the load instead, and maintainer-approval stays a config flag if that ever
changes.

**[`GO_LIVE_PLAYBOOK.md`](GO_LIVE_PLAYBOOK.md) is the new front door for anyone whose goal is "get it
running"** — Stage A (laptop + test server: keys, first index, first grounded answer, the Discord acceptance
checklist, and the P-07 calibration that is still an open guess) then Stage B (cluster: secrets, DNS, apply,
index, webhook, invite, privacy pin). Each step names who can do it, what to run, and how to tell it worked.

## 2026-08-06 (later) — Prompter is released: v0.1.0, then v0.1.1 with the image

**State:** `main` @ the PR #4 merge, pushed. Four PRs merged today (#2 deployment stack + planning, #3 the
runtime image fix, #4 the CI image gate), and Prompter has its **first releases**.

**What happened, in order.** Created the missing `major`/`minor`/`patch` labels — the repo had none, which is
the direct reason PR #1 released nothing. Merged #2 with `minor` → **v0.1.0** cut automatically. Its
`publish-docker` job then failed: `mcr.microsoft.com/dotnet/aspnet:10.0-bookworm-slim` does not exist —
Debian-slim runtime variants stop at .NET 9, so that tag never resolved and the July "image builds
end-to-end" note in this file was wrong (or the tag was pulled since). Fixed to `10.0-noble`, which is what
Studio's services run on, verified by building the image locally *and* starting it (it stops exactly at the
`Discord:Token` startup validation, so the runtime image, publish output and entrypoint are all proven).
Merged as `patch` → **v0.1.1**, the first version with a pullable image: `cratis/prompter:0.1.1` and
`:latest` are on Docker Hub for **amd64 and arm64** (391 MB), and the published artifact was verified by
pulling and running it, not just by trusting the workflow's green tick.

**Gap closed:** nothing built the container until Publish tried to push one. `build.yml` now builds the image
on pull requests (native arch, no push — Publish still does multi-arch) and triggers on `Docker/**`, which
previously triggered nothing at all.

**v0.1.0 is annotated** in its release notes as having no image; use v0.1.1 or later.

**Deploy stayed skipped by design** — the `deployment-configured` gate found no `PULUMI_CONFIG_PASSPHRASE` /
`UPCLOUD_TOKEN` / `Deployment/state`, so it reported a notice instead of failing the release. That is the
whole remaining list to go from "released" to "running": the two secrets, `clusterId` + `ingressHost`, a DNS
record, `scripts/set-secrets.sh` with the five runtime secrets, and `pulumi stack init production`.

## 2026-08-06 — Release readiness assessed; deployment stack built; docs-gap + GitHub-issue surfaces planned

**State:** Branch **`deploy/release-story-and-docs-gap`** off `main` @ `c9ad5ce`, **not pushed**. Release build
**0 warnings**, **278 specs green**. `main` had moved 11 commits since the last entry (workflow bootstrap,
Copilot-instruction sync, package bumps) — the local checkout was fast-forwarded before starting.

**Release readiness — the finding that matters:** **nothing has ever been released.** `gh release list` is
empty and `hub.docker.com/v2/repositories/cratis/prompter` 404s. Cause: `cratis/release-action` only cuts a
release for a merged PR labeled `major`/`minor`/`patch`; PR #1 carried none, so Publish ran, decided
`should-publish=false`, and did nothing — successfully. The first release is a `publish.yml`
`workflow_dispatch` with an explicit version, or the next PR merged with a label. Written up under "Release
mechanics" in [`DEPLOYMENT.md`](DEPLOYMENT.md). Everything else about v1 is unchanged: code-complete, and
gated on (1) Voyage + Anthropic keys — which also gate **P-07 threshold calibration**, the one remaining
quality decision, since `Answering:MinScore` is still a guess; (2) a Discord app + test server; (3) deploy.

**Deployment is no longer a plan — it is code.** New [`Deployment/`](../Deployment/README.md) Pulumi C#
project (in the solution, builds clean in Release): looks the existing UKS cluster up by id, then creates only
namespaced resources in `prompter-production` — Postgres/pgvector StatefulSet, the single-replica bot
Deployment + Service, the two Secrets, and an ingress publishing **only** `POST /reindex`. Plus
`.github/workflows/deploy-production.yml` (pins the tag with `pulumi config set`, `pulumi up`, commits state
back with `[skip ci]`) wired as a `workflow_call` from Publish. Studio's `Deployment/` was read properly this
time and copied where it counts: self-managed `file://./state`, passphrase secrets, `set-secrets.sh`,
self-hosted `cratis` runner.

**Q-5 answered → [D-15](DECISIONS.md) (OPEN, needs the team's nod):** Prompter's Pulumi code lives **here**,
not in Studio's stack — reversing D-11's guess. Evidence: Studio's `deploy-production.yml` pins one version
across every Studio image (so a `prompterImage` entry needs its own workflow + cross-repo dispatch anyway),
and a shared stack would make every Prompter release re-evaluate MongoDB/Chronicle/AuthProxy. Reversal is
cheap by construction — every resource class takes `Provider` + `Namespace` exactly like Studio's own.

**Nothing has been applied.** The stack is unrun infrastructure code. Gates: `PULUMI_CONFIG_PASSPHRASE` +
`UPCLOUD_TOKEN` repo secrets, the `clusterId`/`ingressHost` config values, a DNS record at the cluster load
balancer, and `scripts/set-secrets.sh` with the five runtime secrets. Backups are deliberately not wired
(corpus is rebuildable; the interaction log is anonymous rows) — recorded in the operations table.

**Two new surfaces planned (no code):** **P-44** GitHub-issues surface — `POST /github/webhook` on the
existing Kestrel host, answer `issues.opened` with citations, **silence on refusal**, opt-out label, per-repo
cap; distinct from P-32 (which *ingests* answered issues). **P-45** a "this should be documented" button next
to 👍/👎 that forwards the question text and stores nothing — the click is the consent, so it needs no
privacy-posture change. **P-33** was rewritten around those two feeds, and **[D-14](DECISIONS.md) (OPEN)**
records the actual open question: does Prompter store question text at all? Recommendation: A (never store;
consent in the moment) now, B (refusal-only text, consent notice + short retention) only if the button's
signal proves too thin. **Q-7** added: which repo receives a docs-gap issue.

**Next:** (1) team confirms D-15 and D-14's direction; (2) add the deploy secrets and cut `0.1.0`; (3) keys →
Stage 0 on a test server → P-07 calibration; (4) then `pulumi up`.

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
- `f52a3c8` — planning-doc reconciliation (this entry + BACKLOG).
- `9638eee` **P-42 residual + a review Low** — the background reindex now runs under
  `IHostApplicationLifetime.ApplicationStopping` (cancels cleanly on shutdown, logged as its own outcome), and
  `ReindexAuth` SHA-256-hashes both secrets to a fixed 32 bytes before `FixedTimeEquals` so the compare no
  longer leaks the secret's length. So **P-42 is now fully done.**

**P-43 (git hygiene) — worktrees cleared, branches preserved:** all 19 `.claude/worktrees/agent-*` worktree
directories were removed (all clean; nothing dirty discarded) and the registry pruned to the single main
checkout — no repo commit (that path is gitignored). The 19 `worktree-agent-*` **branches** are intentionally
kept: they are unmerged by ancestry (cherry-picked onto `main` under new hashes), so removing them needs
`git branch -D`, held back as a destructive step on branches this session didn't create. Delete them at will
once you're satisfied `main` carries their content.

**Still excluded (need keys / live / design):** **P-36** (model-`[n]` citations — needs live validation),
**P-41** (eval answer-rate gate + real baseline — needs API keys). **P-40** optional extras (jitter,
`Retry-After`, retrying HttpClient `TaskCanceledException` timeouts) were not taken.

**Next:** decide whether to merge `fix/format-preserve-sources` into `main` + push (a review follow-up,
externally visible on public `main`), and whether to force-delete the 19 preserved `worktree-agent-*` branches.
Then P-07 calibration once keys land.

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
