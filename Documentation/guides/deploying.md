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
laptop or a cluster. The artifacts are the same across hosts. Choose a host for your task rather than
working through the table as sequential setup stages. For development, follow
[Run Prompter locally](running-locally.md); for production, use the deployment project guide. The optional
VM row is not a prerequisite for either path.

Before starting an answer-enabled host, explicitly set the scalar
`Cratis__Prompter__Protection__ConfiguredAllowedGuilds=1182595891576717413`. Only the Cratis guild is
allowed, including for local running and Development. A token and channel settings alone are not enough;
local hosting does not enable a separate private test guild.

| Stage | Where | Good for |
|---|---|---|
| Laptop | `docker compose up -d` plus `dotnet run` | Local ingestion, retrieval, and answering; Discord use restricted to the Cratis guild |
| Simple VM (optional) | Smallest UpCloud VM, Docker Compose | An always-on beta before the cluster work lands |
| Cluster | The shared Cratis UpCloud UKS via Pulumi | Production: automated deploys and observability |

## In production

Prompter is a single-replica Kubernetes Deployment - the Discord gateway wants exactly one connection -
alongside an in-cluster Postgres with pgvector, in the Norway region. Keeping stored data on EU-jurisdiction
infrastructure strengthens the [privacy](../concepts/privacy.md) posture. It exposes `GET /healthz` for probes
and a cluster-internal `POST /reindex` (shared secret); neither has a public route.

Prepare the application token and channel settings with [Discord app setup](discord-setup.md). The
[first-deploy steps](https://github.com/Cratis/Prompter/blob/main/Deployment/README.md#first-deploy) govern
namespace onboarding in Infrastructure, GitHub secrets, and deployment through CI. Run **Reindex - Production**
once after the first deploy. Before inviting questions, complete the privacy-notice step in Discord app
setup. See [Use Prompter on Discord](using-prompter.md) for the supported question surfaces and refusals.
The tracker bridge's inbound GitHub webhook is dormant in production until a public route or relay is decided.

## Private metrics and alerts

Metrics and alerts are opt-in; no alert destination is assumed. Follow [Monitor Prompter](monitoring.md)
for the private-metrics prerequisites, Grafana dashboard, paused alert provisioning, and failure-notification
configuration. The same guide has operator checks for missing telemetry, index failures, spend, and provider
errors once the bot is running.

## Documentation changes never redeploy

App deploys are for code changes only. Documentation changes never redeploy or trigger reindexing.
The **Reindex - Production** workflow picks up published changes daily at 03:52 UTC when scheduled
indexing is enabled (`PROMPTER_SCHEDULED_INDEXING_ENABLED=true`), or a maintainer can run
it on demand on `main`. It creates a one-shot Kubernetes Job running the deployed image's `index` command,
and fails if indexing fails or the crawl is empty. See
[Configuration](../reference/configuration.md) for the endpoints and
[Grounded answers](../concepts/grounded-answers.md) for how freshness works.
