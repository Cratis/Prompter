# Deployment

Prompter's own Pulumi project: its Postgres, its Kubernetes Deployment/Service and the outbound-HTTPS
network policy. There is no public Ingress or DNS record. It targets the shared production Kubernetes
cluster Cratis/Infrastructure's Core stack owns, through a namespace-scoped kubeconfig Core's `AppAccess`
hands over; it does not provision or own the cluster, the `prompter` namespace or the StorageClass. Those
stay Core's (see "Values read from Core" below). The image is published to and pulled from Docker Hub
(`docker.io/cratis/prompter`), which is public, so there is no pull secret.

The structure and the workflows mirror Cratis/Direct's `Deployment/`. This document names where Prompter
differs.

## What gets deployed

```text
namespace prompter (provisioned by Cratis/Infrastructure's Core stack)
├── prompter-postgres        ← Secret: the generated database password
├── postgres-data            ← PersistentVolumeClaim (10Gi, upcloud-maxiops)
├── postgres                 ← Deployment (pgvector/pgvector:0.8.7-pg17, 1 replica, Recreate) + ClusterIP Service
├── prompter-secrets         ← Secret: Discord token, Anthropic + Voyage keys, reindex secret, connection string
├── prompter                 ← Deployment (1 replica, Recreate) + ClusterIP Service on 8080
└── prompter-external-https  ← NetworkPolicy: outbound TCP 443 to public addresses only
```

Re-index Jobs are created by the workflow, not managed as stack resources. The tracker bridge's inbound
GitHub webhook has no public route in production; that part is dormant until a route is decided.

Single replica is a requirement, not sizing: the Discord gateway wants exactly one connection per bot.
Postgres is a Deployment on a claim rather than a StatefulSet because the deploy Role Core grants every app
has no `statefulsets` permission; one replica on a ReadWriteOnce volume with `Recreate` gives the same
single-writer guarantee.

## Offline GitHub App wiring

Optional non-secret keys in `prompter-deployment` are `gitHubAppId`, `gitHubInstallationId`,
`gitHubOwner`, `gitHubInstallationRepositories` (exact comma-separated `owner/repository` names),
`gitHubAppExternalSecretName`, and `gitHubAppKeyReference`. Supply all six or none. Partial or invalid
inputs fail before resources are constructed with a fixed error that does not echo inputs.

This stack only references an existing namespace-local Kubernetes Secret and one named key. It never
reads, imports, or provisions that external Secret or its contents. The pod projects only that key at
`/var/run/prompter/github-app/private-key.pem`, read-only with mode `0440`, UID/GID and fsGroup `1654`.
The API is fixed to `https://api.github.com/`. No configurable API host, PEM content, or key path is accepted.
All known workload credential mappings remain explicit Secret key references; unexpected Secret keys
are not imported as environment variables.

App mode emits the authoritative `InstallationRepositoriesJson` scalar rather than indexed grants,
forces `AllowTokenFallback=false` and `Protection:GitHubWritesEnabled=false`, and grants no new routes
or confirmations. Complete metadata does not authorize writes. With all App inputs absent, no App
environment, key mount, or pod security changes are added; legacy token/webhook mappings remain.

Provisioning, key rotation, installation permissions, runtime identity verification, and checking actual
mount readability remain operator work. None are verified by offline wiring or synthetic specifications;
this is not production activation.

## Private Laya routing configuration

Private routing defaults off: without `layaEndpoint`, the bot uses deterministic fallback and no Laya
policy is created. There is no `Routing.Enabled` setting. This wiring does not activate a live service.

All inputs below are non-secret keys in the `prompter-deployment` Pulumi configuration namespace.
A configured endpoint requires every identity field and an explicit mode; incomplete or invalid settings
fail before deployment resources are constructed, without echoing supplied values.

| Key | Required value for configured routing |
|---|---|
| `layaEndpoint` | HTTP or HTTPS service origin, with the service port if non-default; no credentials, path, query or fragment |
| `layaAllowedHost` | Independently approved exact `<service>.<namespace>.svc.cluster.local` host |
| `layaService`, `layaNamespace` | Exact service identity matching the approved host; no destination defaults |
| `layaApp`, `layaEnvironment` | Exact destination pod labels |
| `layaBackendPort` | Verified destination TCP backend port, not an assumed copy of the service port |
| `layaMode` | Explicit `Active` or `Shadow`; shadow sampling remains zero |

The bot receives only the validated routing origin, independent host, mode, confidence `0.75`, margin
`0.15` and shadow percentage `0`. Unknown runtime modes and non-finite or out-of-range thresholds are
rejected without remote calls. Transport limits, one-attempt behavior, no redirects and no proxy use
remain unchanged. Index Jobs receive no Laya grant.

The separate `prompter-private-routing` egress policy selects only the bot and its environment. Its one
peer combines the destination namespace and both pod labels (AND), with only the verified TCP backend
port. For a service on port 80 targeting backend 8080, the policy grants 8080 only, not both ports.
Operators must verify Service/targetPort mapping and CNI policy semantics before supplying that port.
Destination ingress approval and the live private Decision API path remain unverified activation blockers;
this configuration does not create destination ingress or authorize activation.

## State

Self-managed on UpCloud Object Storage (S3-compatible): `Deployment/Pulumi.yaml`'s `backend.url` points at
the `cratis-pulumi-state-production` bucket Core provisions and the Studio and Direct stacks also use. No
Pulumi Cloud/Service is involved. Secrets in the stack are encrypted with the passphrase secrets provider, so
every `pulumi` command needs `PULUMI_CONFIG_PASSPHRASE` in the environment, plus `AWS_ACCESS_KEY_ID` and
`AWS_SECRET_ACCESS_KEY` set to Core's `stateBackendAccessKeyId` and `stateBackendSecretAccessKey` stack
outputs. On Pulumi < 3.261.0, also set `AWS_REQUEST_CHECKSUM_CALCULATION=when_required` and
`AWS_RESPONSE_CHECKSUM_VALIDATION=when_required` (a known aws-sdk-go-v2 regression otherwise fails every state
read and write). Store the passphrase in a password manager and as a GitHub Actions secret; if it is lost, the
stack's secrets cannot be decrypted.

Only `Pulumi.production.yaml` lives in git. Unlike Direct's, Prompter's stack is new: the encryption salt is
generated when the stack is created and must be committed, which is what the `init` operation does, once.

## Values read from Core

Read live via `StackReference("organization/core-deployment/production")`:

| Output | Used for |
|---|---|
| `prompterNamespace` | A guard: the deployment fails when Core has not onboarded Prompter |

Non-secret values are in [`Pulumi.production.yaml`](Pulumi.production.yaml) with comments:

| Key | Notes |
|---|---|
| `prompterImage` | Pinned by the deploy to `docker.io/cratis/prompter:<version>` |
| `storageClassName` | Defaults to `upcloud-maxiops` (Core's StorageClass) |
| `postgresStorageSizeGb` | Volumes can grow, never shrink |
| `askChannelId`, `helpForumChannelId` | Discord channel ids; omit to disable that surface |
| `answeringRepositories`, `issueNotifyChannelId` | Tracker bridge; optional |

## Credentials Prompter owns directly

Secrets are never in stack config. The workflows hand GitHub Actions secrets to the stack as environment
variables, the same way Direct does. Because this repository is public, they are **`production` environment
secrets**, not repository secrets: the environment only accepts deployments from `main`, so pull request
workflows (including the Eval run) never see the production credentials.

| GitHub secret | Environment variable read by `Program.cs` | Notes |
|---|---|---|
| `PULUMI_CONFIG_PASSPHRASE` | `PULUMI_CONFIG_PASSPHRASE` | The shared Cratis passphrase |
| `STATE_BACKEND_ACCESS_KEY_ID`, `STATE_BACKEND_SECRET_ACCESS_KEY` | `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` | Core's state backend outputs |
| `KUBECONFIG` | `PROMPTER_KUBECONFIG` | From `scripts/generate-prompter-kubeconfig.sh` |
| `DISCORD_TOKEN` | `PROMPTER_DISCORD_TOKEN` | From the Discord application |
| `ANTHROPIC_SECONDARY_API_KEY` | `PROMPTER_ANTHROPIC_SECONDARY_API_KEY` | Optional; custom field `woksin-org-api-token-prod` in `Anthropic - Prompter - API Key`; never the primary note |
| `ANTHROPIC_API_KEY`, `VOYAGE_API_KEY` | `PROMPTER_ANTHROPIC_API_KEY`, `PROMPTER_VOYAGE_API_KEY` | The two model providers |
| `REINDEX_SECRET` | `PROMPTER_REINDEX_SECRET` | `openssl rand -hex 32`; guards only the in-cluster `/reindex` endpoint via `X-Reindex-Secret` |
| `PROMPTER_GITHUB_TOKEN`, `PROMPTER_GITHUB_WEBHOOK_SECRET` | same | Optional; tracker bridge. GitHub forbids secret names starting with `GITHUB_`, so these keep the prefix |

The Postgres password is generated by Pulumi (`RandomPassword`) and kept in state, so no person chooses it.
`scripts/set-github-secrets.sh` sets these environment secrets from Bitwarden (`--dry-run` first); values go
to `gh secret set` on stdin and are never printed. The reindex secret and Discord token are Bitwarden notes
("Prompter - Reindex Secret", "Discord - Prompter - Bot token"). The Eval workflow keeps its own
repository-level `ANTHROPIC_API_KEY` and `VOYAGE_API_KEY`; use separate, budget-capped keys there.

Secondary-key wiring does not activate fallback: keep task `AllowSecondary=false` until the shared
reservation/accounting integration and independent provider caps are installed and evaluated. A missing
custom field makes the provisioning script fail before writing any GitHub secrets; it never copies the
primary note into the secondary slot. Runtime deployments without a configured secondary remain primary-only. Neither key is logged,
used in rollout annotations directly, or exposed in stack outputs. The existing secret-tainted configuration
fingerprint includes the secondary key through the complete secret map. Offline wiring fixtures run with
`node --test Deployment/scripts/specs/provider-credentials.mjs` and stub every Bitwarden/GitHub call.

## Local operator access (read-only)

Direct lets an operator run `pulumi up` locally through `scripts/pulumi-local.sh`. Prompter does not:
production `preview`, `up` and `refresh` run only in CI. From a workstation you can look, using credentials
from Bitwarden loaded into one shell, with no file holding the values:

```bash
cratis-bw-unlock                            # writes ~/.config/bitwarden/session
source Deployment/scripts/load-credentials.sh
(cd Deployment && pulumi stack output --stack production)
kubectl --kubeconfig "$PROMPTER_KUBECONFIG_PATH" -n prompter get pods
prompter_credentials_clear
```

The loader reads one Bitwarden secure note (override its name with `PROMPTER_BW_ENV_ITEM`) and takes only the
five whitelisted names; see the header of [`scripts/load-credentials.sh`](scripts/load-credentials.sh).

## First deploy

1. Cratis/Infrastructure's Core stack onboards Prompter (the `prompter` namespace, deploy identity and the
   `prompterNamespace` output). Merge and deploy that Infrastructure change first.
2. An operator with Infrastructure's admin access runs `scripts/generate-prompter-kubeconfig.sh`, then
   `scripts/set-github-secrets.sh`.
3. Run `Deploy - Production` with `operation=init` once. It creates the stack and commits the encryption salt.
4. Run it with `operation=deploy` and the latest released `version`.
5. Run **Reindex - Production** (`gh workflow run reindex-production.yml --ref main`) once the Voyage key works.
6. Check `/healthz` through the pod logs, and install the Discord application per
   [`DISCORD_INTEGRATION.md`](../Planning/DISCORD_INTEGRATION.md).

## CI

`.github/workflows/publish.yml`'s `deploy` job runs `pulumi up` against this project on every release,
mirroring Cratis/Direct: checkout `main` fresh, refuse to roll production back, pin the just-published image,
`pulumi up` with `refresh: true`. A `concurrency` group stops two releases from applying at once. It is skipped
(with a notice) until the secrets above exist and the stack has been initialised.

`.github/workflows/deploy-production.yml` is the manual counterpart, with Direct's inputs:

| Operation | What it does |
|---|---|
| `deploy` | Pins the image to `version` and runs `pulumi up`; `refresh` and `target` are recovery-only |
| `release-stack-lock` | Cancels an interrupted update and releases its lock |
| `init` | Prompter only: creates the stack once and commits the encryption salt |

`version` must be a SemVer such as `0.2.7` or `1.0.0-rc.1`; anything else is rejected before it reaches a shell.
**Documentation changes never deploy anything**: the nightly refresh or an on-demand maintainer run picks
up published changes.

### Re-indexing

Run **Reindex - Production** on `main` with `gh workflow run reindex-production.yml --ref main`, or let its
nightly schedule run at **03:52 UTC**. It uses the `production` environment's namespace-scoped `KUBECONFIG`
to create a one-shot Job running the deployed image's `index` command. It refuses to overlap an active
index Job. The Job reads only the connection string and Voyage key from `prompter-secrets` and has the
same outbound HTTPS access as the bot, without joining the bot's Service. The database advisory lease
also serializes CLI, HTTP and Jobs across processes; contention fails immediately. The shared workflow
concurrency group is `prompter-production-operations`, with no cancellation or deletion of an active Job.

The workflow fixes Voyage pricing at **$0.06/million tokens**, with **$0.10/run** and **$0.50/day**
ceilings; these are not repository variables. Missing/zero pricing or ceilings disable paid work.
Configure the equivalent `Ingestion` options on the bot for HTTP/CLI runs; all runs reserve against the
same database UTC-day allowance. Reservations include `Voyage.IndexMaxRetries + 1` attempts (6),
use UTF-8 bytes plus framing (not characters/4), and are retained after
failure or unknown provider outcomes. They are upper bounds, not measured provider usage or invoices.

The daily Job passes `--scheduled`: at most one admitted scheduled run per UTC day, with a
retry-inclusive **1M tokens/run** ceiling (about 167 KB of changed chunk text). Plain manual runs remain incremental. Full runs have a default
**25M tokens/run** ceiling and require the workflow's `full` input plus literal confirmation **FULL REINDEX**
and the existing `production` environment approval. The script equivalent uses `PROMPTER_INDEX_MODE=full`
and `PROMPTER_INDEX_CONFIRMATION='FULL REINDEX'`. Never schedule full mode. Token ceilings are configurable
with `Ingestion.MaxIncrementalTokens` and `Ingestion.MaxFullTokens`; raising them requires budget approval.
No rebuild is implied by the initial observed corpus size.

**Enable the daily schedule:** set the non-secret **repository** variable
`PROMPTER_SCHEDULED_INDEXING_ENABLED=true` for the daily 03:52 UTC run. An environment-scoped variable
is not visible to the job-level gate. Jobs enable indexing independently of the disabled bot pod.
Set `PROMPTER_ALERT_WEBHOOK` to an HTTPS JSON receiver for failure notifications; without it only
GitHub's failed-schedule notifications and freshness alerts remain. Documentation changes never
redeploy or trigger reindexing. Unchanged chunks are never re-embedded.

For the weekly metadata-only docs-gap digest, set the non-secret Pulumi key
`prompter-deployment:maintainerChannelId` and deploy. The bot needs View Channel and Send Messages
in that channel. No channel means no query or post. The default slot is Monday 08:00 UTC;
day and hour are not plumbed through Pulumi. A restart computes the next future slot, so a restart
across a slot may skip that week. Counts cover the preceding seven days (or less if retention is
shorter); no question text is stored. Empty weeks still post a confirmation.

The same `maintainerChannelId` enables the asker-consented "This should be documented" and "Request this"
buttons. Two more optional, non-secret Pulumi keys switch on the other Discord behaviors; unset means off:
`prompter-deployment:maintainerRoleId` (a mentionable role pinged by "Ask a human") and
`prompter-deployment:answerInThreadChannelIds` (comma-separated channel ids where answers open a thread).
Non-numeric values fail the deployment.

The indexer preflights the entire crawl before any paid embedding, stages vectors in memory, then publishes
all changed chunks and stale deletion in one transaction on the lease-owning session. Partial fetches,
embedding failures, cancellation or lost sessions cannot publish a partial corpus. This requires enough
Job memory for the approved corpus. Do not deploy a new query embedding model before its compatible staged
full corpus is ready: incremental indexing rejects model/dimension/profile changes. The migration records
the legacy shipped `voyage-4/1024/markdown-v1` profile; confirm that matches the existing corpus before upgrade.
Dimension changes additionally require a compatible schema migration; bump the profile version for future
chunking changes. Price verification and query-model rollout remain operator responsibilities.

Offline Job manifest specs run with `bash Deployment/scripts/run-index-job.spec.sh` (no cluster access).
See [indexing integration specs](../Specs/Integration/README.md) for disposable-Postgres verification.

A crash, provider error, timeout, missing summary or zero-page crawl fails the workflow. Only the fixed
index summary and Kubernetes status appear in public Actions logs. Finished Jobs retain their logs for
24 hours. Load credentials with `scripts/load-credentials.sh`, then read raw logs locally:

```bash
kubectl --kubeconfig "$PROMPTER_KUBECONFIG_PATH" -n prompter logs job/<job-name>
```

After rotating the Voyage key, run `scripts/set-github-secrets.sh`, then **Deploy - Production** with the
currently deployed version to update `prompter-secrets` before re-indexing. Any change to the bot's consumed
secret map automatically rolls its single pod via a secret-tainted SHA-256 `checksum/configuration`
pod-template annotation; the annotation contains no raw secret values. Updating GitHub secrets alone
does not update the Job's credentials. Scheduled workflows in public repositories can be disabled after
60 days without activity; if needed, re-enable with `gh workflow enable reindex-production.yml`.

### Postgres backups

Encrypted logical backups and isolated restore verification are opt-in. Follow
[Postgres backups](POSTGRES-BACKUPS.md) for the region, lifecycle, separate identities,
key custodian, pinned tooling image, activation gates and required recovery drill.
Neither backup resources nor scheduled operations are enabled by default.

## Verify

```bash
kubectl -n prompter get pods,svc,jobs
```
