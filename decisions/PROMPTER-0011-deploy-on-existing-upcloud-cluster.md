---
id: PROMPTER-0011
title: Deploy on the existing UpCloud cluster, Studio-style
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-07-15
decider: woksin
applies-to:
  - "Deployment/**"
enforced-by: "not verified — the Deployment/ Pulumi project exists; nothing automated prevents another topology"
review-trigger: "Managed Postgres replacing in-cluster Postgres, or the UpCloud cluster being replaced or retired."
fallback: "Managed Postgres, if pgvector support checks out and in-cluster Postgres proves annoying."
---

Historical decision D-11, 2026-07-15. Where the Pulumi code lives is settled in [PROMPTER-0015](PROMPTER-0015-pulumi-stack-lives-in-this-repo.md).

## Decision

Prompter deploys to the UpCloud UKS cluster (region `no-svg1`, Stavanger/Norway) that Studio's Pulumi stack manages, following Studio's conventions: Pulumi C#, self-managed in-repo state, passphrase-protected configuration, `UPCLOUD_TOKEN`, version-pinned images via `pulumi config set`, and a deploy workflow called from Publish.

This supersedes the original standalone-Hetzner topology.

## Grounds

- No new infrastructure to operate.
- Observability (Loki/Grafana) and backup patterns already exist.
- The Norway region strengthens [PROMPTER-0008](PROMPTER-0008-gdpr-posture.md).
- Marginal run cost drops to the LLM API alone (about $1–10/month), since the cluster, storage and backups are already paid for. This supersedes the ≤ €15/month standalone-box figure cited in [PROMPTER-0002](PROMPTER-0002-build-do-not-buy.md).

Postgres + pgvector runs in-cluster with object-storage backups, mirroring the cluster's MongoDB precedent.

## Fallback

Managed Postgres, if pgvector support checks out and in-cluster proves annoying.
