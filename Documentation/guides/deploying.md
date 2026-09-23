---
title: Deploy Prompter
description: How Prompter runs in production and where to find the operational runbook.
---

Prompter runs in production on the Cratis UpCloud Kubernetes cluster that also hosts Studio. This page is
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
| Cluster | The Cratis UpCloud UKS via Pulumi | Production: automated deploys and observability |

## In production

Prompter is a single-replica Kubernetes Deployment - the Discord gateway wants exactly one connection -
alongside an in-cluster Postgres with pgvector, in the Norway region. Keeping stored data on EU-jurisdiction
infrastructure strengthens the [privacy](../concepts/privacy.md) posture. It exposes `GET /healthz` for probes
and `POST /reindex` (shared secret) for documentation refreshes. Follow the
[first-deploy steps](https://github.com/Cratis/Prompter/blob/main/Deployment/README.md#first-deploy) to configure
the existing cluster and host, supply encrypted secrets, preview the stack, and deploy it. Index the corpus
once after the first deploy and complete [Discord app setup](discord-setup.md) before inviting questions.

## Documentation changes never redeploy

App deploys are for code changes only. When the documentation changes, nothing is redeployed - the
Documentation build calls `POST /reindex` and the corpus updates in place. See
[Configuration](../reference/configuration.md) for the endpoints and
[Grounded answers](../concepts/grounded-answers.md) for how freshness works.
