---
id: PROMPTER-0015
title: Prompter's Pulumi stack lives in this repository
status: accepted
stage: implemented
class: contract
reversibility: reversible
decided: 2026-11-05
decider: woksin
applies-to:
  - "Deployment/**"
enforced-by: "not verified — the Deployment/ folder holds the Pulumi project; nothing automated prevents moving it"
review-trigger: "The cluster being replaced under a different name, or a proposal to move the resources into Studio's Deployment/Services."
---

Historical decision D-15, originally open on 2026-08-06. Accepted on 2026-11-05 by woksin: the stack stays in the repository's `Deployment/` folder.

Answers the open question left by [PROMPTER-0011](PROMPTER-0011-deploy-on-existing-upcloud-cluster.md) and reverses its recommendation (otherwise unchanged: UpCloud UKS `no-svg1`, Pulumi C#, Studio conventions). That record guessed Prompter should become a workload entry in Studio's `Deployment/` stack. Reading Studio's deployment code (`Studio` @ `4abb7ab7`) turned up three facts that point the other way.

## Grounds

1. **Studio's deploy workflow pins one version for the whole platform.** `deploy-production.yml` takes a single `version` input and loops it over `studioImage`/`studioAdminImage`/`studioLobbyImage`/`studioCatalogImage`/`llmImage`. There is no per-workload release path, so a `prompterImage` entry would still need its own workflow in Studio's repo plus a cross-repo dispatch (and a PAT) from Prompter's Publish. The "one place pins every image" benefit assumed earlier is not available to a repo with its own release cadence.
2. **A shared stack means a shared blast radius.** `pulumi up` there re-evaluates the UKS cluster and node group, Percona MongoDB with its PBM backups, Chronicle, Core/Admin/Lobby/Catalog, two AuthProxies, the OAuth2 proxy, Vault, Loki/Grafana/Promtail/OTel and the ingresses. Releasing a Discord bot should not be able to move any of that, and a Studio release should not carry Prompter's database.
3. **Nothing forces co-location.** Prompter needs no cluster-scoped resources: it looks the existing cluster up by name through the UpCloud provider's `GetKubernetesCluster` invoke (the same call Studio uses internally to get its kubeconfig) and creates only namespaced objects inside `prompter-production`. The shared pieces (NGINX ingress controller, the cert-manager `ClusterIssuer`, Promtail's all-namespace log shipping) stay owned by Studio and are consumed by reference, which is also how Prompter gets logs in Grafana for free.

## Decision

A `Deployment/` Pulumi project in this repo, project `prompter-deployment`, stack `production`, deploying into the cluster Studio owns. Everything else follows Studio verbatim, deliberately: self-managed `file://./state` committed to Git, the passphrase-protected configuration provider, a script that sets the encrypted configuration values, and a `deploy-production.yml` that pins the image tag with `pulumi config set`, runs `pulumi up` on the self-hosted `cratis` runner, and commits the updated state back with `[skip ci]`.

## Consequences

Accepted trade-offs: two stacks now touch one cluster (bounded by the namespace rule above, since Prompter creates nothing cluster-scoped); a second passphrase and state directory to look after; and if the cluster is ever replaced under a different name, Prompter's `clusterName` config has to be re-pointed.

Reversal is cheap: every resource class takes a `Provider` + `Namespace` exactly like Studio's own services do, so moving them into `Studio/Deployment/Services` later is a `Program.cs` wiring change, not a rewrite.
