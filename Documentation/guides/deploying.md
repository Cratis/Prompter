---
title: Deploy Prompter
description: How Prompter runs in production and where to find the operational runbook.
---

Prompter runs in production on the shared Cratis UpCloud Kubernetes cluster, which Cratis/Infrastructure owns and Studio and Direct also use. This page is
the map; the public [deployment project guide](https://github.com/Cratis/Prompter/blob/main/Deployment/README.md)
covers the Pulumi stack, configuration, first deploy, and release workflow.

## You can run it anywhere with internet

Because the bot dials out to Discord (see the gateway mechanics in
[Set up the Discord app](discord-setup.md)), it needs no public IP or ingress and runs identically from a
laptop or a cluster. The artifacts are the same at every stage, so nothing is throwaway:

| Stage | Where | Good for |
|---|---|---|
| Laptop | `docker compose up -d` plus `dotnet run` | Trying it end to end on a test server, all bot development |
| Simple VM (optional) | Smallest UpCloud VM, Docker Compose | An always-on beta before the cluster work lands |
| Cluster | The shared Cratis UpCloud UKS via Pulumi | Production: automated deploys and observability |

## In production

Prompter is a single-replica Kubernetes Deployment - the Discord gateway wants exactly one connection -
alongside an in-cluster Postgres with pgvector, in the Norway region. Keeping stored data on EU-jurisdiction
infrastructure strengthens the [privacy](../concepts/privacy.md) posture. It exposes `GET /healthz` for probes
and a cluster-internal `POST /reindex` (shared secret); neither has a public route. Follow the
[first-deploy steps](https://github.com/Cratis/Prompter/blob/main/Deployment/README.md#first-deploy) to onboard
the namespace in Infrastructure, supply the GitHub secrets, and deploy it through CI. Run **Reindex - Production**
once after the first deploy and complete [Discord app setup](discord-setup.md) before inviting questions.
The tracker bridge's inbound GitHub webhook is dormant in production until a public route or relay is decided.

## Private metrics and alerts

See [Monitor Prompter](monitoring.md) for opt-in private metrics, the Grafana dashboard, paused alert
provisioning, failure-notification configuration, and operator runbooks. No alert destination is assumed.

## Documentation changes never redeploy

App deploys are for code changes only. Documentation changes never redeploy or trigger reindexing.
The **Reindex - Production** workflow picks up published changes daily at 03:52 UTC when scheduled
indexing is enabled (`PROMPTER_SCHEDULED_INDEXING_ENABLED=true`), or a maintainer can run
it on demand on `main`. It creates a one-shot Kubernetes Job running the deployed image's `index` command,
and fails if indexing fails or the crawl is empty. See
[Configuration](../reference/configuration.md) for the endpoints and
[Grounded answers](../concepts/grounded-answers.md) for how freshness works.
