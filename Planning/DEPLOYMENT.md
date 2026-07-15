# Deployment — production runbook

How Prompter runs in production: **on the existing UpCloud Kubernetes cluster that runs Studio** (decision
D-11), following Studio's deployment conventions. Implementation order is
[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) M5. Study `Studio/Deployment/` and
`Studio/Documentation/deployment/` before touching anything — that repo is the reference implementation.

## The staging ladder — you don't need the cluster to try it

Because the bot **dials out** to Discord (see the gateway mechanics in
[`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md)), it runs identically from anywhere with internet — no
public IP, no ingress, no cluster required. Stage accordingly:

| Stage | Where | Good for | What it takes |
|---|---|---|---|
| **0 · Laptop** | `docker compose up -d` + `cd Source && dotnet run` on a dev machine | Trying it out end-to-end on a **test Discord server**, all of M3 development, demoing to the team | Discord test app token + API keys in env vars. Free, running in minutes. Stops when the laptop sleeps — never for the real community |
| **1 · Simple VM** (optional) | Smallest UpCloud VM in the existing account, Docker Compose, no Kubernetes/Pulumi | An always-on **beta on the real server** before M5 is built | ~€5–10/mo, one `docker compose up -d`; manual re-index (`dotnet run -- index`) or a cron hitting `/reindex` |
| **2 · Cluster (D-11)** | Studio's UpCloud UKS via Pulumi | Production: automated deploys, observability, backups, the webhook chain | M5 work; the end state |

**Recommended path:** Stage 0 now — it's also the cheapest way to burn down the NetCord-beta unknowns (P-11,
P-12) before any infra exists. Skip Stage 1 unless the community beta needs to run always-on before M5 is
ready; if so, stay inside the existing UpCloud account rather than adding a new vendor. Stage 2 when M5 lands.
The artifacts are identical at every stage (same image, same compose file locally), so nothing is throwaway.

## Topology

Prompter joins the **UpCloud UKS cluster** (region `no-svg1`, Norway) that Studio's Pulumi stack manages:

- **The bot** — one k8s Deployment (single replica; the Discord gateway wants exactly one connection) using
  Studio's `SimpleWorkload` pattern, image `cratis/prompter` (or the private registry, matching however
  `studio-llm` images are hosted). Exposes `GET /healthz` (liveness/readiness probes) and `POST /reindex`
  (shared secret) — the reindex route published through the existing ingress/load balancer so the
  Documentation build can reach it.
- **Postgres + pgvector** — in-cluster StatefulSet with a persistent volume, mirroring how the cluster
  already runs MongoDB, with backups to UpCloud Object Storage the same way MongoDB's S3 backups are wired.
  (Alternative: UpCloud Managed PostgreSQL if it supports the `vector` extension — verify before choosing;
  in-cluster is the recommendation because it matches the MongoDB precedent and the corpus is rebuildable.)
- **Observability for free** — logs flow into the existing Loki/Grafana via Promtail; add a simple Grafana
  panel (questions/day, refusal rate) once interactions accumulate.

Being in `no-svg1` also strengthens the GDPR story from D-8: all stored data (interactions included) stays in
Norway on an EU-jurisdiction provider; the only external processors remain the Anthropic API (answers) and
Voyage (embedding text of public docs).

## Deploy flow (mirrors Studio's)

1. **Release the app**: merged PR → `publish.yml` builds and pushes the versioned image (exactly as today).
2. **Deploy the version**: a `deploy-production.yml` modeled on Studio's — `workflow_call` from Publish +
   manual `workflow_dispatch(version)` — pins the image tag with `pulumi config set` and runs `pulumi up`,
   then commits the updated self-managed Pulumi state back to the repo (`file://./state`, passphrase
   provider, `PULUMI_CONFIG_PASSPHRASE` + `UPCLOUD_TOKEN` secrets — same secret names as Studio).
3. **Where the Pulumi code lives** is Q-5 (open): either a `Deployment/` project in this repo targeting the
   existing cluster, or a `prompterImage` entry in Studio's `Deployment/` stack next to `llmImage` /
   `prologueApiImage`. Recommendation in D-11: **join Studio's stack** — that is the established pattern for
   platform services on this cluster, one place pins every image. Revisit if Prompter's release cadence needs
   to decouple.

Remember the separation that makes this cheap ([`CONTENT_AND_FRESHNESS.md`](CONTENT_AND_FRESHNESS.md)):
**app deploys are for code changes only** — documentation changes never redeploy anything; they trigger the
`/reindex` endpoint and the corpus updates in place.

## One-time setup (P-26, revised for UpCloud)

1. GitHub repo `Cratis/Prompter` + secrets: `DOCKER_USERNAME`/`DOCKER_PASSWORD` (or registry creds matching
   Studio's registry), `PAT_DOCUMENTATION`; plus — wherever the Pulumi code lands — access to
   `PULUMI_CONFIG_PASSPHRASE` and `UPCLOUD_TOKEN`.
2. First image release via `publish.yml`.
3. Pulumi additions (per Q-5 resolution): Prompter workload + Postgres StatefulSet + ingress route +
   config/secrets (`Cratis__Prompter__…` env vars from k8s secrets: Discord token, Anthropic key, Voyage key,
   reindex secret, connection string).
4. `pulumi up`, run the first index (`/reindex` or a one-off `index` job), install the Discord app per
   [`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md).

## Recurring operations

| Concern | How |
|---|---|
| **App update** | Merge → Publish → deploy workflow pins the new version → `pulumi up` (Studio pattern; badge/state committed) |
| **Docs freshness** | `/reindex` webhook from the Documentation build + nightly schedule — no deploys involved |
| **Backups** | Postgres → UpCloud Object Storage, same wiring as the cluster's MongoDB backups; the corpus is rebuildable from cratis.io, so **interactions** are the only data that matters |
| **Monitoring** | k8s probes on `/healthz`; logs in Loki/Grafana (already collected); weekly glance at refusal rate + feedback ratio |
| **Secrets rotation** | k8s secrets via the Pulumi stack (passphrase-encrypted config), rotated with `pulumi config set --secret` + `pulumi up` |
| **Data subject requests** | Delete by hashed user id: `DELETE FROM interactions WHERE user_hash = …` (hash the requester's Discord id with the scheme in `Discord/UserHash.cs`) |

## Superseded plan

The original v1 plan targeted a standalone Hetzner CAX11 with Docker Compose (~€6.50/mo) — superseded by
D-11 (existing UpCloud cluster: no new infra to operate, existing observability/backups/registry, Norway
region). The compose file in this repo remains the **local development** environment only.
