# Prompter Deployment

Pulumi C# project that runs Prompter on the **UpCloud** managed Kubernetes cluster (UKS, zone `no-svg1`,
Norway) that Studio's Pulumi stack owns.

Two things make this different from a stock Pulumi project — read them before running anything:

1. **Self-managed state, committed to Git.** There is no Pulumi Cloud account. State lives in-repo under
   `state/` (a `file://` backend) and secrets are encrypted with a passphrase. This mirrors Studio, which is
   the reference implementation for this cluster.
2. **This stack does not own the cluster.** Studio's stack creates the UKS cluster, its node group, the NGINX
   ingress controller, cert-manager's `letsencrypt-prod` ClusterIssuer, the `upcloud-maxiops` StorageClass and
   Promtail's log shipping. This stack looks the cluster up by id and creates **only namespaced resources**
   inside `prompter-production`. That rule is what keeps two stacks on one cluster from fighting — see
   decision [D-15](../Planning/DECISIONS.md).

## What gets deployed

```text
UpCloud UKS (owned by Studio's stack)
└── namespace prompter-production
    ├── Secret  prompter-postgres      ← database password
    ├── Secret  prompter-secrets       ← Discord token, Anthropic + Voyage keys, reindex secret,
    │                                     connection string (keys are the Cratis__Prompter__… paths)
    ├── StatefulSet postgres (pgvector/pgvector:pg17) + headless Service + 10Gi volume
    ├── Deployment prompter (1 replica, Recreate) + ClusterIP Service on 8080
    └── Ingress   prompter-ingress     ← TLS host, publishes ONLY POST /reindex
```

Single replica is a requirement, not sizing: the Discord gateway wants exactly one connection per bot.

## Stack

| Project | Stack | Backend | Public host |
|---------|-------|---------|-------------|
| `Deployment/` (`prompter-deployment`) | `production` | `file://./state` | `prompter.cratis.studio` (config) |

## Configuration

Non-secret values live in [`Pulumi.production.yaml`](Pulumi.production.yaml) with comments. Secrets are set
with [`scripts/set-secrets.sh`](scripts/set-secrets.sh) and stored as passphrase-encrypted `secure:` values in
the same file, which is what makes it safe to commit.

| Key | Kind | Notes |
|---|---|---|
| `clusterId` | config | UKS cluster id to deploy into (`upctl kubernetes list`) |
| `ingressHost` | config | Public host; needs a DNS record at the cluster load balancer |
| `prompterImage` | config | Pinned by the deploy workflow to `cratis/prompter:<version>` |
| `storageClassName` | config | Defaults to `upcloud-maxiops` (Studio's StorageClass) |
| `postgresStorageSizeGb` | config | Volumes can grow, never shrink |
| `askChannelId`, `helpForumChannelId` | config | Discord channel ids; omit to disable that surface |
| `postgresPassword` | secret | `openssl rand -base64 32` |
| `discordToken` | secret | From the Discord application |
| `anthropicApiKey`, `voyageApiKey` | secret | The two model providers |
| `reindexSecret` | secret | `openssl rand -hex 32`; the Documentation build sends it as `X-Reindex-Secret` |

## First deploy

```bash
cd Deployment
export PULUMI_CONFIG_PASSPHRASE=...            # pick one; store it in a password manager + repo secret
export UPCLOUD_TOKEN=...                       # or UPCLOUD_USERNAME + UPCLOUD_PASSWORD

pulumi stack init production                   # creates local state under ./state
# fill in clusterId + ingressHost in Pulumi.production.yaml first
export POSTGRES_PASSWORD=... DISCORD_TOKEN=... ANTHROPIC_API_KEY=... VOYAGE_API_KEY=... REINDEX_SECRET=...
./scripts/set-secrets.sh
pulumi preview                                 # review
pulumi up
git add Deployment/state Deployment/Pulumi.production.yaml && git commit -m "Update production state"
```

Then index the corpus once — `POST /reindex` with the secret, or run the image's `index` mode as a one-off
job — and install the Discord application per [`DISCORD_INTEGRATION.md`](../Planning/DISCORD_INTEGRATION.md).

## CI/CD

`Deploy - Production` (`.github/workflows/deploy-production.yml`) pins `prompterImage` to the released
version, runs `pulumi up` against the file backend, and commits the updated state back to `main` with
`[skip ci]`. Publish calls it automatically after pushing the image; `workflow_dispatch` redeploys any
version by hand. It needs two repository secrets: `PULUMI_CONFIG_PASSPHRASE` and `UPCLOUD_TOKEN`.

**Documentation changes never deploy anything** — they trigger a re-index. See
[`CONTENT_AND_FRESHNESS.md`](../Planning/CONTENT_AND_FRESHNESS.md).
