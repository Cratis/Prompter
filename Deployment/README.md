# Deployment

Prompter's own Pulumi project: its Postgres, its Kubernetes Deployment/Service, the Ingress serving
`prompter.cratis.studio` and the outbound-HTTPS network policy. It targets the shared production Kubernetes
cluster Cratis/Infrastructure's Core stack owns, through a namespace-scoped kubeconfig Core's `AppAccess`
hands over; it does not provision or own the cluster, the `prompter` namespace, the ingress controller,
cert-manager or the StorageClass. Those stay Core's, read live via StackReference (see "Values read from
Core" below). The image is published to and pulled from Docker Hub (`docker.io/cratis/prompter`), which is
public, so there is no pull secret.

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
├── prompter-ingress         ← Ingress: TLS host, publishes ONLY POST /reindex and the GitHub webhook
└── prompter-external-https  ← NetworkPolicy: outbound TCP 443 to public addresses only
```

Single replica is a requirement, not sizing: the Discord gateway wants exactly one connection per bot.
Postgres is a Deployment on a claim rather than a StatefulSet because the deploy Role Core grants every app
has no `statefulsets` permission; one replica on a ReadWriteOnce volume with `Recreate` gives the same
single-writer guarantee.

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
| `ingressLoadBalancerEndpoint` | Exported as `ingressDnsTarget`, the CNAME target for `prompter.cratis.studio` |

Non-secret values are in [`Pulumi.production.yaml`](Pulumi.production.yaml) with comments:

| Key | Notes |
|---|---|
| `ingressHost` | Public host; needs a DNS CNAME to the `ingressDnsTarget` output |
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
| `ANTHROPIC_API_KEY`, `VOYAGE_API_KEY` | `PROMPTER_ANTHROPIC_API_KEY`, `PROMPTER_VOYAGE_API_KEY` | The two model providers |
| `REINDEX_SECRET` | `PROMPTER_REINDEX_SECRET` | `openssl rand -hex 32`; the Documentation build sends it as `X-Reindex-Secret` |
| `PROMPTER_GITHUB_TOKEN`, `PROMPTER_GITHUB_WEBHOOK_SECRET` | same | Optional; tracker bridge. GitHub forbids secret names starting with `GITHUB_`, so these keep the prefix |

The Postgres password is generated by Pulumi (`RandomPassword`) and kept in state, so no person chooses it.
`scripts/set-github-secrets.sh` sets these environment secrets from Bitwarden (`--dry-run` first); values go
to `gh secret set` on stdin and are never printed. The reindex secret and Discord token are Bitwarden notes
("Prompter - Reindex Secret", "Discord - Prompter - Bot token"). The Eval workflow keeps its own
repository-level `ANTHROPIC_API_KEY` and `VOYAGE_API_KEY`; use separate, budget-capped keys there.

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
5. Create the DNS CNAME `prompter.cratis.studio` pointing at the `ingressDnsTarget` output.
6. Index the corpus once with `POST https://prompter.cratis.studio/reindex` and the `X-Reindex-Secret` header,
   check `/healthz` through the pod logs, and install the Discord application per
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
**Documentation changes never deploy anything**: they trigger a re-index. See
[`CONTENT_AND_FRESHNESS.md`](../Planning/CONTENT_AND_FRESHNESS.md).

## Verify

```bash
kubectl -n prompter get pods,svc,ingress,certificate
pulumi stack output reindexUrl
```
