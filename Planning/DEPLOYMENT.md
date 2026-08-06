# Deployment — production runbook

How Prompter runs in production: **on the existing UpCloud Kubernetes cluster that runs Studio** (decisions
D-11 and D-15), following Studio's deployment conventions. Implementation order is
[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) M5. `Studio/Deployment/` and
`Studio/Documentation/deployment/` are the reference implementation — this stack was written from them.

> **Working through this for the first time?** [`GO_LIVE_PLAYBOOK.md`](GO_LIVE_PLAYBOOK.md) sequences every
> step — laptop proof-out, calibration, then the cluster — with a verification per step. This document is the
> reference behind it.
>
> **Status (2026-08-06):** the infrastructure code exists and compiles — [`Deployment/`](../Deployment/README.md)
> (Pulumi C#) plus `deploy-production.yml`, called from Publish. **Nothing has been applied yet, and no image
> has ever been published**: Docker Hub has no `cratis/prompter` repository and the repo has no releases,
> because `cratis/release-action` only cuts a release for a merged PR labeled `major`/`minor`/`patch` and the
> one merged PR carried none. Read "Cutting the first release" below before anything else.

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

Everything lives in its own namespace, `prompter-production`, and **nothing cluster-scoped is declared** —
the cluster, node group, NGINX controller, cert-manager issuer, `upcloud-maxiops` StorageClass and Promtail
all belong to Studio's stack and are referenced by name (D-15).

- **The bot** — one k8s Deployment, single replica with the `Recreate` strategy (the Discord gateway wants
  exactly one connection, so two pods would double every answer), image `cratis/prompter:<version>` from
  public Docker Hub — no pull secret needed. Readiness probes `GET /healthz` (database + gateway);
  **liveness deliberately does not** — `/healthz` goes unhealthy during a Discord outage, and restarting the
  pod in a loop would neither fix Discord nor keep the re-index endpoint alive, so liveness is a TCP check.
  `POST /reindex` is the only path published through the existing ingress/load balancer; `/healthz` stays
  cluster-internal.
- **Postgres + pgvector** — in-cluster single-replica StatefulSet (`pgvector/pgvector:pg17`, the same image
  local compose and the eval workflow use) with a `upcloud-maxiops` volume, mirroring how the cluster already
  runs MongoDB. Backups are not wired in the first cut — see the operations table below for why, and for when
  that stops being true. (Alternative: UpCloud Managed PostgreSQL if it supports the `vector` extension — Q-6;
  in-cluster is the recommendation because it matches the MongoDB precedent and the corpus is rebuildable.)
- **Observability for free** — logs flow into the existing Loki/Grafana via Promtail; add a simple Grafana
  panel (questions/day, refusal rate) once interactions accumulate.

Being in `no-svg1` also strengthens the GDPR story from D-8: all stored data (interactions included) stays in
Norway on an EU-jurisdiction provider; the only external processors remain the Anthropic API (answers) and
Voyage (embedding text of public docs).

## Release mechanics — how a version comes into existence

`publish.yml` runs on every closed pull request, but it only *releases* something under conditions worth
knowing, because the first attempt silently produced nothing:

- **A merged PR labeled `major`, `minor` or `patch`** → `cratis/release-action` computes the next semantic
  version from the latest release (or the highest existing tag), creates the GitHub release, and sets
  `should-publish=true`, which is what gates the Docker build and the deploy.
- **A merged PR with no such label** → no release, no image, no deploy. The workflow still runs and still
  reports success. This is what happened to PR #1 and why Docker Hub has no repository yet.
- **A PR that was closed without being merged** → never releases, whatever labels it carries.
- **`workflow_dispatch` with an explicit `version`** → releases that version directly. This is the manual
  path, and the simplest way to cut the very first one.

### Cutting the first release

1. Confirm the Docker Hub credentials reach this repo (they are org-level and proven by Chronicle.Mcp).
   The `cratis/prompter` repository does not exist yet — the first push creates it if the account allows
   auto-create; otherwise create it by hand first.
2. Add the deploy secrets `PULUMI_CONFIG_PASSPHRASE` and `UPCLOUD_TOKEN` (repo or org level) — without them
   the deploy job fails after a successful publish.
3. Run **Publish** with `workflow_dispatch`, version `0.1.0` (or merge a PR labeled `minor`).
4. Publish builds and pushes `cratis/prompter:0.1.0` + `:latest`, then calls **Deploy - Production** with
   that version.

## Deploy flow (mirrors Studio's)

1. **Release the app**: `publish.yml` builds and pushes the versioned image (above).
2. **Deploy the version**: [`deploy-production.yml`](../.github/workflows/deploy-production.yml) —
   `workflow_call` from Publish + manual `workflow_dispatch(version)` — pins the image tag with
   `pulumi config set`, runs `pulumi up` on the self-hosted `cratis` runner, and commits the updated
   self-managed Pulumi state back to the repo (`file://./state`, passphrase provider,
   `PULUMI_CONFIG_PASSPHRASE` + `UPCLOUD_TOKEN` — the same secret names Studio uses).
3. **Where the Pulumi code lives** was Q-5, and is now answered by **[D-15](DECISIONS.md)**: a `Deployment/`
   project **in this repo**, its own stack, deploying into the cluster Studio owns. Reading Studio's actual
   deploy workflow is what settled it — it pins one version across every Studio image, so a `prompterImage`
   entry there would have needed its own workflow and a cross-repo dispatch anyway, while making every
   Prompter release re-evaluate MongoDB, Chronicle and the AuthProxies. The full argument is in D-15; the
   stack itself is documented in [`Deployment/README.md`](../Deployment/README.md).

Remember the separation that makes this cheap ([`CONTENT_AND_FRESHNESS.md`](CONTENT_AND_FRESHNESS.md)):
**app deploys are for code changes only** — documentation changes never redeploy anything; they trigger the
`/reindex` endpoint and the corpus updates in place.

## One-time setup (P-26, revised for UpCloud)

1. **Repository secrets** — `DOCKER_USERNAME`/`DOCKER_PASSWORD` and `PAT_DOCUMENTATION` are org-level and
   confirmed reaching this repo. Add `PULUMI_CONFIG_PASSPHRASE` (invent one, store it in the password
   manager) and `UPCLOUD_TOKEN` (or `UPCLOUD_USERNAME`/`UPCLOUD_PASSWORD`).
2. **First image release** via `publish.yml` — see "Cutting the first release" above.
3. **Stack bootstrap** — in `Deployment/`: fill in `clusterId` (`upctl kubernetes list`) and `ingressHost`,
   `pulumi stack init production`, run `scripts/set-secrets.sh` with the five runtime secrets exported,
   `pulumi preview`, `pulumi up`, then commit `Deployment/state` + `Deployment/Pulumi.production.yaml`.
   Full walkthrough in [`Deployment/README.md`](../Deployment/README.md).
4. **DNS** — point the `ingressHost` record at the cluster's existing ingress load balancer, or cert-manager
   cannot complete the ACME challenge and the TLS secret never issues.
5. **First index** — `POST /reindex` with the shared secret (or run the image's `index` mode as a one-off
   job), then install the Discord app per [`DISCORD_INTEGRATION.md`](DISCORD_INTEGRATION.md).
6. **Webhook wiring** — add the `/reindex` call to the Documentation repo's deploy job with the same secret.

## Recurring operations

| Concern | How |
|---|---|
| **App update** | Merge a labeled PR → Publish → deploy workflow pins the new version → `pulumi up` (Studio pattern; state committed) |
| **Rollback** | Run **Deploy - Production** by hand with the previous version — the stack pins whatever tag it is given |
| **Docs freshness** | `/reindex` webhook from the Documentation build + nightly schedule — no deploys involved |
| **Backups** | **Not wired yet.** The corpus is rebuildable from cratis.io and the interaction log is anonymous rows (D-13), so losing the volume costs one re-index and some aggregate history — the reason the first cut ships without backups. Add PBM-style object-storage backups alongside the MongoDB precedent if the interaction history ever becomes analysis-critical |
| **Monitoring** | k8s probes on `/healthz`; logs in Loki/Grafana (already collected); weekly glance at refusal rate + feedback ratio |
| **Secrets rotation** | k8s secrets via the Pulumi stack (passphrase-encrypted config), rotated with `pulumi config set --secret` + `pulumi up` |
| **Data subject requests** | Nothing to action: the interaction log stores no personal data or identifier (D-13), so there is no per-user data to export or delete |

## Superseded plan

The original v1 plan targeted a standalone Hetzner CAX11 with Docker Compose (~€6.50/mo) — superseded by
D-11 (existing UpCloud cluster: no new infra to operate, existing observability/backups/registry, Norway
region). The compose file in this repo remains the **local development** environment only.
