# Go-live playbook — from a released image to Prompter answering on the Cratis Discord

Every step from where we are **today** to the bot answering real questions on the real server. Written to be
worked through in order by a human — Sindre or Einari — with no reading of the other planning documents
required. Each step says who can do it, exactly what to run, and how you know it worked.

Nothing here is code work. The bot is code-complete and released; what remains is credentials, a test run,
one calibration, and the cluster. Stage C switches on the tracker bridge, which ships in the same image and
stays off until its credentials exist.

Each stage has a GitHub issue with the same checklist, so progress can be tracked and split between
people: [Stage 0 · #10](https://github.com/Cratis/Prompter/issues/10),
[Stage A · #6](https://github.com/Cratis/Prompter/issues/6),
[Stage B · #7](https://github.com/Cratis/Prompter/issues/7),
[Stage C · #8](https://github.com/Cratis/Prompter/issues/8).

**Stage 0 comes first and belongs to whoever owns the Cratis accounts** — the two model-provider keys and the
Discord application, which should be org-owned rather than personal.
[`ORG_SETUP.md`](ORG_SETUP.md) is that checklist, about 30 minutes; everything below assumes it is done.

## Readiness — what is proven, and what has never run

The honest state as of **2026-08-06**, because "ready" means different things for testing and for production.

**Proven, by actually doing it:**

- **v0.2.0 released** — `cratis/prompter:0.2.0` and `:latest` on Docker Hub (amd64 + arm64). The published
  image was pulled and started; it comes up and stops exactly where it should with no configuration.
- Release build 0 warnings, 389 specs green; the container image is built on every pull request.
- The **outbound GitHub path** was exercised against the real API: the exact issue-filing payload works,
  labels that do not exist yet (`from-discord`) are created on use, and the duplicate-search response matches
  what the code parses.

**Never executed — this is the whole gap:**

| Never run | What that means |
|---|---|
| The corpus has never been indexed | Retrieval and answering have never produced one real answer. Every claim about answer quality is theory until Stage A. |
| No Discord surface has touched a live gateway | `/ask`, mentions, ask channel, forum auto-reply, feedback buttons, rate limiting, long-answer splitting, the apology path — spec-verified, never observed. |
| `/issue` end to end | The GitHub half is proven; the Discord half (ephemeral defer → preview → button → file) has not run. |
| `Answering:MinScore` | Still the committed guess. This is the quality gate. |
| `pulumi up` | The stack has never been applied — cluster lookup, StorageClass reference, certificate issuance, volume binding, the probes. |
| `deploy-production.yml` | Never run — runner availability, environment protections, the state commit-back. |
| `Eval/baseline.json` | A placeholder, so the CI quality gate is not yet real. |

**So: ready for testing today. Not ready for the real server until Stage A and B have run** — the gap is
things that have not been *done*, not things that have not been built.

## The stages, and the order that matters

| Stage | What it proves | Time | Cost |
|---|---|---|---|
| **A · Laptop + test server** | The whole product actually works — retrieval, answers, citations, every Discord surface | An afternoon | Free tier + a few cents of API |
| **B · Cluster** | It stays up without a laptop, and the docs stay fresh automatically | An hour, plus DNS propagation | Marginal — the cluster is already paid for |
| **C · Tracker bridge** | Filing issues from Discord, answering new ones, notifying maintainers | Under an hour | Nothing |

Do not skip A. The bot dials *out* to Discord, so a laptop is a completely legitimate way to run the real
thing against a test server — and every problem you find there is one you are not debugging through
`kubectl logs`. **Stage A is also where the one open quality question gets answered** (step A6).

**The gates, in order:**

1. **Stage A in full, including A6.** Do not put an uncalibrated threshold in front of the community: it
   either answers questions the docs do not cover or refuses ones they do, and a bot's first impression is
   hard to undo.
2. **Stage B**, once A is clean.
3. **A week of answering only** on the real server before any tracker writing. Watch the refusal rate and the
   feedback ratio; the threshold is a config value, so tuning it is not a redeploy.
4. **Stage C in order** — C1 any time (it needs nothing from Prompter), then C2 (filing, which a person
   initiates and can be closed), and **C3 last, on one repository**, because answering is the only thing here
   that writes into other people's trackers.

**What is most likely to bite, in order:** the refusal threshold (A6); slash-command registration and the new
`/issue` interaction flow, the least-exercised NetCord surface; the first `pulumi up`, where certificate
issuance and volume binding usually stumble; and answering on a real tracker, which is the most publicly
visible thing this bot does.

---

## Stage A — prove it on a laptop

### A1 · Get the two API keys · *from Stage 0*

Both keys are created on the org accounts in [`ORG_SETUP.md`](ORG_SETUP.md) steps 1–2 and handed over through
the password manager. If you have them, export and continue; if not, that checklist is the blocker.

```bash
export Cratis__Prompter__Voyage__ApiKey=...
export Cratis__Prompter__Anthropic__ApiKey=...
```

**Verify:** both variables are set in the shell you will use for the next steps. Nothing is committed —
these live in your environment or in a git-ignored `Source/appsettings.Development.json`.

### A2 · Index the corpus for the first time · *anyone with the keys*

```bash
docker compose up -d                      # Postgres + pgvector on localhost:5432
cd Source
dotnet run -- index
```

**Verify:** the run ends with a one-line summary (pages, embedded, unchanged, removed, duration) and no
error. Then run it **again** — the second run must report **0 embedded / all unchanged**. That is the
incremental-indexing contract, and it is what makes the re-index webhook cheap later.

**If it fails:** a 401 means the Voyage key is not in the environment. A connection error means Postgres is
not up (`docker compose ps`). ~870 pages take a few minutes on a first run.

### A3 · Ask it something from the terminal · *anyone*

```bash
dotnet run -- ask "How do I append an event in Chronicle?" --verbose
```

**Verify:** you see the retrieved passages with scores, page URLs and heading paths, then a grounded answer
that cites its sources. This is the first proof that ingestion → retrieval → answering works end to end.

**Also try one out-of-scope question** — `dotnet run -- ask "what is the best pizza in Oslo"` — and confirm
it *refuses* rather than inventing an answer. The command exits non-zero on a refusal, which is what makes
the next step scriptable.

### A4 · Get the Discord token and channel ids · *from Stage 0*

The application, its Team ownership, the privileged intent, the exact permission set and the private test
server are all [`ORG_SETUP.md`](ORG_SETUP.md) step 3 — done once, by whoever owns the Cratis accounts. The
behavior contract behind those choices is in
[`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md#discord-application-setup-runbook--team-action-p-17ap-26).

What you need here is the bot token plus the two channel ids:

```bash
export Cratis__Prompter__Discord__Token=...
export Cratis__Prompter__Discord__AskChannelId=...        # the test text channel
export Cratis__Prompter__Discord__HelpForumChannelId=...  # the test forum channel
```

**Verify:** the bot appears offline in the test server's member list. It comes online in the next step.

### A5 · Run the bot and work the checklist · *anyone*

```bash
cd Source
dotnet run
```

The bot comes online and Kestrel serves `/healthz` on <http://localhost:8080/healthz>. Now walk the list —
this is the acceptance test for everything M3 built, and none of it has ever run against a live Discord:

- [ ] `@Prompter how do I define a projection?` in any channel → an answer with sources, as a reply
- [ ] A plain message in the ask channel (no mention) → answered
- [ ] `/ask <question>` → "thinking…" appears within 3 s, then the answer arrives
- [ ] A new post in the test **forum** channel → auto-answered in-thread, plus the "a human will follow up" line
- [ ] 👍 / 👎 buttons on an answer → clicking acknowledges ephemerally
- [ ] Six questions in ten minutes from one account → the sixth gets the friendly rate-limit reply
- [ ] A deliberately long answer arrives complete (split across messages, sources on the last)
- [ ] `/issue something is broken` → with no GitHub token set, it replies (privately) that filing is not
      configured. That is the check that the **command registered at all** — the interaction surface is what
      is least exercised, and this proves it before any credential is involved.
- [ ] Stop Postgres (`docker compose stop postgres`) and ask again → an apology, not silence. Start it again.

**If a slash command does not appear at all,** give it time before debugging: the first *global* registration
can take up to an hour to propagate. Guild-scoped registration is instant, which is why the test server is
the right place to iterate.

**If something misbehaves,** that is exactly what this stage is for — file it and fix before Stage B.

### A6 · Calibrate the refusal threshold (P-07) · *anyone, ~1 hour* — **do not skip**

`Answering:MinScore` currently ships as a **guess**. Too low and Prompter answers questions the docs do not
cover; too high and it refuses questions they do. This is the one quality decision that is still open.

Run ~20 real in-scope questions (from Discord history, the FAQ, or
[`Eval/golden-questions.yaml`](../Eval/golden-questions.yaml)) and ~5 deliberately out-of-scope ones through
`ask --verbose`, and note the **top passage score** for each. Pick a threshold that sits cleanly between the
two clusters, set it, and re-run to confirm.

```bash
dotnet run -- ask "<question>" --verbose | head -5      # the top score is the first passage's
```

**Verify:** with the chosen value, all 5 out-of-scope questions refuse and all 20 in-scope ones answer.
Record the number and the evidence in `IMPLEMENTATION_PLAN.md` under M2.2, and commit the new default.

### A7 · Generate the real eval baseline (P-19/P-41) · *anyone, optional but cheap*

```bash
dotnet run --project Eval
```

Writes a scored report to `Eval/results/`. Copy the scores into `Eval/baseline.json` and commit — this turns
the eval CI gate from a placeholder into a real one, so a future prompt change that makes answers worse fails
its pull request.

---

## Stage B — put it on the cluster

### B1 · Add the two deploy secrets · *Einari, or whoever administers the org*

Repository (or org) secrets on `Cratis/Prompter`:

| Secret | Value |
|---|---|
| `PULUMI_CONFIG_PASSPHRASE` | Invent one, store it in the password manager. Losing it means re-encrypting every stack secret. |
| `UPCLOUD_TOKEN` | The same UpCloud credential Studio's deploy uses (or `UPCLOUD_USERNAME`/`UPCLOUD_PASSWORD`). |

**Also confirm** the self-hosted runner labelled `[self-hosted, linux, cratis]` is available to this
repository. If it is not, the deploy job **queues silently forever** rather than failing — switch
`runs-on` to `ubuntu-latest` in `.github/workflows/deploy-production.yml` (the cluster's control plane
accepts connections from anywhere, so a hosted runner reaches it).

### B2 · Fill in the two cluster values · *Einari*

```bash
upctl kubernetes list        # the id of the UKS cluster Studio runs on
```

In [`Deployment/Pulumi.production.yaml`](../Deployment/Pulumi.production.yaml) set `clusterId` and
`ingressHost` (e.g. `prompter.cratis.studio`), and — from Stage A — the two Discord channel ids of the
**real** server.

### B3 · Point DNS at the cluster · *Einari*

Create the `ingressHost` record at the cluster's existing ingress load balancer, the same way Studio's hosts
are wired. **Verify:** the name resolves to the load balancer address. Do this *before* B5 — cert-manager
cannot complete its challenge without it, and a failed issuance is slow to retry.

### B4 · Set the runtime secrets · *whoever holds the keys*

```bash
cd Deployment
export PULUMI_CONFIG_PASSPHRASE=...
export POSTGRES_PASSWORD=$(openssl rand -base64 32)
export REINDEX_SECRET=$(openssl rand -hex 32)
export DISCORD_TOKEN=...  ANTHROPIC_API_KEY=...  VOYAGE_API_KEY=...
pulumi stack init production
./scripts/set-secrets.sh
```

**Verify:** `Pulumi.production.yaml` now holds `secure: v1:...` entries. Those are passphrase-encrypted and
safe to commit. Keep `REINDEX_SECRET` — B7 needs it.

### B5 · Deploy · *Einari*

```bash
pulumi preview      # read it: it should create a namespace, a StatefulSet, a Deployment, a Service, an Ingress
pulumi up
git add Deployment/state Deployment/Pulumi.production.yaml
git commit -m "Deploy 0.1.1 and update production Pulumi state"
```

**Verify:** `kubectl -n prompter-production get pods` shows `postgres-0` and a `prompter-*` pod both Running,
and `curl https://<ingressHost>/reindex` returns **401** (no secret header) rather than a connection error or
a certificate warning.

From here on, releases deploy themselves: merge a pull request labelled `major`/`minor`/`patch` and Publish
builds the image and calls the deploy workflow.

### B6 · Index in production · *Einari*

```bash
curl -X POST -H "X-Reindex-Secret: $REINDEX_SECRET" https://<ingressHost>/reindex
```

**Verify:** returns 202, and the pod logs show the index run completing with its summary line. A second call
while one is running returns 409 — that is correct.

### B7 · Wire the freshness webhook · *Einari*

Add a step at the end of the `Cratis/Documentation` repo's docs-site deploy job that calls the same URL with
the same secret (stored as a secret in that repo). This is what makes "docs merged → Prompter knows" happen
without anyone thinking about it. Documentation changes never redeploy Prompter — they only re-index.

### B8 · Invite the bot to the real server · *Sindre or Einari*

Install the application on the Cratis server (`1182595891576717413`) with the same minimal permission set,
create `#ask-prompter` and confirm the help forum channel, and put their ids into the stack config
(`askChannelId`, `helpForumChannelId`) — then `pulumi up` again.

**Verify:** ask it something real in `#ask-prompter` and get a cited answer.

### B9 · Pin the privacy notice (P-23) · *Sindre or Einari*

Pin a short message in `#ask-prompter` naming the bot, what it processes (your question goes to the Anthropic
API to generate an answer), and what it stores (**nothing identifying — no message content, no user id**).
The wording is already written at [`Documentation/concepts/privacy.md`](../Documentation/concepts/privacy.md);
link to it rather than restating it. This is a claim we can make because of D-13, so it is worth making
visibly.

---

---

## Stage C — the tracker bridge (optional, any time after A)

The issue features ship in the same image and stay **off** until their credentials exist, so none of this
blocks going live. Turn them on when the bot is answering reliably.

**Gate:** C1 needs nothing and can be done at any point. C2 and C3 wait until the bot has been answering on
the real server for about a week — long enough to know the answers are good before any of them are attached
to a public tracker. **C3 goes last and on one repository**, because it is the only step that writes into
somebody else's tracker.

### C1 · Plain issue notifications in Discord — no code, five minutes · *Sindre or Einari*

Do this one first regardless of the rest; it needs nothing from Prompter and keeps working when Prompter is
down.

1. In the Discord channel you want them in: **Edit Channel → Integrations → Webhooks → New Webhook**, copy
   the URL.
2. **Append `/github` to that URL.**
3. On each repository: **Settings → Webhooks → Add webhook**, paste the URL, content type
   `application/json`, and select the **Issues** event.

**Verify:** open a throwaway issue and watch it appear in the channel. Close it again.

### C2 · Let Prompter file issues from Discord · *whoever administers the org*

The outbound half of this was verified against the real API on 2026-08-06 — the filing payload works and
GitHub creates the `from-discord` label on first use, so no repository needs preparing. What has not run is
the Discord half, which is what this step proves.

1. Create the token. A **fine-grained personal access token** is the simplest thing that works: *Settings →
   Developer settings → Fine-grained tokens*, resource owner **Cratis**, select the repositories issues may
   be filed in, permission **Issues: Read and write**, nothing else. A GitHub App is the better long-term
   identity — see the note below — but the token gets you running today and swapping is a config change.
2. Set it: `export GITHUB_TOKEN=...` then `./scripts/set-secrets.sh` (Stage B4), and `pulumi up`.

**Verify:** `/issue something small and harmless` in Discord → a preview appears → **Create issue** → the link
works, and the issue carries the `from-discord` label.

> **Token or App?** The token is one secret and no setup, but it acts as *you*: issues will show your name as
> the author, it expires on the schedule you pick, and its rate limit is your personal one. A GitHub App gets
> Prompter its own identity ("Prompter filed this"), no expiry, and per-repository installation — worth doing
> once the volume justifies the hour it costs. The code sends either as a bearer token, so switching is
> changing one secret.

### C3 · Let Prompter answer new issues · *whoever administers the org* — **one repository first**

1. `export GITHUB_WEBHOOK_SECRET=$(openssl rand -hex 32)`, run `./scripts/set-secrets.sh`.
2. Set `answeringRepositories` in the stack config to the repositories that opt in, and optionally
   `issueNotifyChannelId` for the enriched announcement (the one that says whether the docs already answer
   it). `pulumi up`.
3. On each opted-in repository: **Settings → Webhooks → Add webhook** →
   `https://<ingressHost>/github/webhook`, content type `application/json`, the same secret, **Issues** event.

**Verify:** open a throwaway issue asking something the docs cover → Prompter comments with citations. Open
one asking about something they do not → **it stays silent**, and the maintainer channel says so. That
asymmetry is the design, not a bug.

Leave it on one repository until a handful of real issues have been answered well. Widening is adding a name
to `answeringRepositories` and a webhook; narrowing again is removing them — but a bad comment on a
maintainer's bug report is not so easily taken back.

## Week one — what to watch

- `kubectl -n prompter-production logs deploy/prompter -f` on the first real day. Answers should take 5–15 s.
- The `interactions` table: refusal rate and feedback ratio. A refusal rate that looks high means the
  threshold from A6 wants revisiting — it is a config value, not a redeploy.
- `/healthz` on a free uptime monitor.

## Then what

The tracker bridge ([D-16](DECISIONS.md#d-16--prompter-bridges-discord-and-the-trackers--2026-08-06)) is
built and shipped — Stage C above switches it on. What is left of that idea is **P-47**: handing genuinely
mechanical issues to a coding agent. That one needs its own decision record before any repository opts in,
because it puts machine-written pull requests in front of maintainers; the guardrails it must carry are in
the backlog entry.
