---
title: Monitor Prompter
description: Monitor Prompter with private aggregate OpenTelemetry metrics, dashboards and reindex failure alerts, and troubleshoot spend, indexing and provider errors.
---

Prompter exports **only aggregate metrics** through Core's private OpenTelemetry collector to Prometheus.
There is no `/metrics` listener, public scrape route, push gateway, automatic HTTP instrumentation, or
GenAI prompt/completion capture. Labels are fixed allowlists: provider, model (unrecognized models become
`other`), task, logical account, operation, outcome, token kind, and usage knowledge. No message text,
user/channel identifiers, operation UUIDs, URLs, exception text, credentials, or provider response bodies
are recorded. Existing logs and interaction retention are separate from this export.

For a first setup, start with [Enable private metrics](#enable-private-metrics), then
[Dashboard and alert activation](#dashboard-and-alert-activation). [Scheduled reindex failure delivery](#scheduled-reindex-failure-delivery)
uses a separate receiver. For an incident, go directly to [Missing telemetry](#missing-telemetry),
[Index failure](#index-failure), [Spend and protection](#spend-and-protection), or [Provider errors](#provider-errors).

## Enable private metrics

Exporting defaults **off**. Keep it disabled until the collector, network path, and end-to-end delivery
are validated; enabling the option alone is not sufficient.

For a standalone process, configure all three options: `Cratis__Prompter__Metrics__Enabled=true`,
`Cratis__Prompter__Metrics__Endpoint`, and `Cratis__Prompter__Metrics__AllowedHost`. The independently
approved host must have the form `<service>.<namespace>.svc.cluster.local`, and the endpoint must be
exactly `http://<service>.<namespace>.svc.cluster.local:4318/v1/metrics` (OTLP HTTP/protobuf), using that
same host. No credentials, query string, fragment, alternate scheme, port, or path are accepted.
Set `Enabled=false` to disable exporting.

For deployment, explicitly configure these nonsecret `prompter-deployment` stack keys before the normal
authorized deployment:

- `metricsEnabled`: enable only after validation; set to `false` to disable exporting.
- `metricsEndpoint` and `metricsAllowedHost`: the exact approved endpoint and matching host above.
- `metricsService` and `metricsNamespace`: the actual collector Service and namespace; never guess a namespace.
- `metricsApp` and `metricsEnvironment`: the collector pods' destination app and environment labels.
- `metricsBackendPort`: the backend port verified from the actual collector Service, not assumed from the
  endpoint's port 4318.

The egress rule combines the destination namespace and pod app/environment selectors in the **same peer**:
namespace **AND** app **AND** environment must match. It permits the verified backend port. Core still owns
DNS, namespace policies, collector ingress and Prometheus scraping; validate the complete path and delivery
before activation. No public ports or ingress are added.

The collector destination label `metricsEnvironment` is distinct from the workload's OTLP resource
environment. The deployment forwards its configured `environment` to `Cratis__Prompter__Metrics__Environment`;
standalone processes can set that option directly. The OTLP resource includes `service.name=prompter` and
`deployment.environment.name`, allowing only `production`, `staging`, `development`, or `test`; unset or
unrecognized values become `unknown`. These are deployment configuration values, never request or user
labels.

The runtime exports every 30 seconds, with a three-second exporter deadline. CLI commands flush on
normal exit. Short-lived process counters are best-effort (a kill can lose them). Durable database
aggregates, read every 30 seconds by the bot, are the authoritative source for index health and reserved
spend. If the database refresh fails, the last snapshot age rises rather than becoming a fresh zero.

## Dashboard and alert activation

[Monitoring/dashboard.json](https://github.com/Cratis/Prompter/blob/main/Monitoring/dashboard.json) imports
into Core Grafana using its existing `prometheus` datasource UID. The dashboard uses the collector's
normal Prometheus name translation (`.` to `_`, counter `_total`, histogram `_seconds`).
It covers attempts, logical operations, latency, tokens, usage knowledge, estimated cost, reserved spend,
protection denials, retries, quota fallback/reset, and index outcome/freshness.

Operator acceptance must confirm translated metric names, query evaluation, firing/resolution and actual
notification receipt in a nonproduction test environment before activation.

Core currently disables Prometheus Alertmanager and the push gateway. This repository therefore ships
Grafana-managed alert provisioning, **paused by default**, not an unconfigured notification service.
The definitions are in
[Monitoring/rules.json](https://github.com/Cratis/Prompter/blob/main/Monitoring/rules.json) (JSON-compatible
Prometheus rule format). Render Grafana file provisioning with:

```bash
node Monitoring/grafana-alerts.mjs
```

The operator must supply a real destination/contact point and a Grafana notification policy matching
`service=prompter`, choose the environment and responsible owner, and provision the rendered JSON under
Grafana's `provisioning/alerting` directory through Infrastructure's normal workflow. After configuring
and testing that route, render with `--enable` to unpause the rules. Do not put receiver URLs or tokens
in this repository. The Prompter deployment cannot modify Core Grafana. Importing the dashboard alone
does not activate alerts. With no destination configured, leave the rules paused.

These are initial operational thresholds, not measured percentiles. Review them against traffic.
Spend tiers cover the interactive total, each provider, and the separate indexing ceiling. Incremental
index tiers apply only when no confirmed full run was admitted that UTC day. On a confirmed full day,
exposure at or above the incremental ceiling raises a warning; exposure above the admitted full-run
ceiling remains critical.

| Condition | Required duration |
|---|---|
| Telemetry absent/stale | five minutes |
| Failed/cancelled pipeline after latest success | two minutes |
| No success in more than 30 hours | 15 minutes |
| 70% of a UTC-day exposure ceiling (warning) | five minutes |
| 90% of a UTC-day exposure ceiling (critical) | five minutes |
| Hard cap reached (100%) | no pending delay |
| Provider errors above 20% over ten minutes with at least five attempts | five minutes |
| At least three 429s over ten minutes | two minutes |
| Any auth/quota/exhausted-overload event | one minute |
| Ten admission/protection denials over ten minutes | five minutes |

Expected empty provider series do not alert. The dedicated missing-telemetry rule handles absent index
snapshots; Grafana datasource execution failures alert rather than silently resolving.

## Scheduled reindex failure delivery

The **Reindex - Production** workflow waits for the one-shot indexing Job's actual completion and fails
on Job failure. A failure/cancellation step optionally sends a generic JSON notification to the GitHub
repository secret `PROMPTER_ALERT_WEBHOOK`, which must be set for failure alerts. An unset secret
prints a disabled notice and succeeds; only GitHub's failed-scheduled-run notification and freshness
alerts remain.
This is a dedicated HTTPS JSON receiver, **not a Discord webhook**. It receives only `service`, `event`,
`run_url` (the Actions run), and `status`; never Job logs or application errors. Destination and response
are never printed. Delivery is bounded, retries twice, and fails the notification step if unsuccessful.
A runner that is killed before this step cannot deliver: durable freshness alerts cover that blind spot.

The pipeline decorator records attempts and success/failure/cancellation around the protected indexer,
so manual CLI, scheduled Jobs and detached `/reindex` work share the same database counters and timestamps.
HTTP acceptance is never counted as success. Aggregate writes have independent five-second deadlines;
a telemetry failure never replaces the original indexing result. A killed process cannot record its
completion; the last-success freshness rule remains the fallback. Historical successes before the
migration are unknown (timestamp zero) until the first new successful run.

## What spend means

`prompter_state_anthropic_usd` and `prompter_state_voyage_usd` read the existing interactive UTC-day spend
ledger. `prompter_state_index_usd` reads indexing's metered exposure: measured usage plus in-flight
and unknown byte bounds. `prompter_state_index_full_ceiling_usd` records the highest admitted full-run
day ceiling (0 when none), written atomically with exposure by whichever process reserved, including
the index Job. These are
**reserved exposure, not provider invoices**; unknown usage retains conservative reservations and is
never refunded by metrics. Interactive and indexing ceilings are separate and alert separately.
Known input/output tokens produce `prompter_provider_estimated_usd_total` only with verified matching
model prices and no nonzero unpriced additional token counts. This is an upper-price estimate, not
billing reconciliation. Unknown usage increments `knowledge=unknown`, without invented zero tokens/cost.
Cache-read and reasoning counts are subsets, not extra input/output; do not sum all token kinds.
Cache creation is emitted only for recognized provider count keys, with no raw additional-count labels.
Partial token information is not treated as complete usage. Indexing without verified protection pricing
still has its durable indexing exposure, but no known-usage USD estimate.

## Missing telemetry

Check the enabled flag, bot health, private collector reachability and Core's collector scrape health.
An old snapshot indicates database/migration trouble, not zero spend. Confirm the additive
`v1_5_0.sql` migration and the additive `v1_9_0.sql` full-ceiling column migration ran; old binaries
ignore the new column. Check `prompter_events_total{reason="telemetry_failure"}`. Never open a public
scrape path to work around private networking. Roll back by disabling metrics; the additive aggregate
table can remain for old binaries to ignore.

## Index failure

Inspect the linked Actions run and Job exit status through the normal authorized operations process.
Check index pricing/limits and the database lease; do not bypass full-reindex consent or raise budgets
blindly. Compare `last_attempt`, `last_success`, failures and cancellations. A successful invocation
clears the last-failure alert. A killed/hung process is detected by stale success, not a fabricated result.
After a fresh install, one successful refresh is required to establish freshness.

## Spend and protection

Compare reserved exposure with the configured global/provider and index ceilings. Unknown usage may
legitimately retain exposure. Investigate repeated admission/concurrency denials without collecting
identities. Reduce workload, disable paid work, or review verified prices/ceilings with the operator;
never automatically raise caps in response to an alert. For a confirmed full-day warning, check the
Reindex workflow run that confirmed `FULL REINDEX` and compare exposure with the recorded full-run
ceiling. A runaway critical means exposure exceeds that admitted ceiling; without a confirmed full
run, the incremental tiers remain active. Investigate the reserving process and stop unexpected paid
work; never raise caps to clear either alert.

When spend looks wrong, the first response is `/answering pause`: it stops every answer, webhook answer and delayed forum sweep immediately without a redeploy (other replicas pick it up within 30 seconds). The transitions are visible as `protection` events with the reasons `paused` and `resumed`. Resume with `/answering resume` once the cause is understood.

## Provider errors

HTTP 401/403 is `auth`, 402 is `quota`, 429 is `rate_limit`, and verified Anthropic 503/529 overload is `capacity`.
A suspension without a distinct safe machine signal is included in auth/provider failure; no raw error
text is inspected to guess suspension or account allocation. Follow provider status and account health
through approved tooling. Quota-confirmed secondary fallback is measured separately and does not change
account authorization; capacity retries never authorize fallback. Do not add retries, switch accounts,
expose keys or copy provider response bodies while investigating.

An answer that reaches its output-token limit counts as `prompter_events_total{source="anthropic",reason="output_limit"}`
and logs a warning saying whether a shortened answer was recovered. A rising count means answers need more
output headroom. Raise `Anthropic:Answer:MaxOutputTokens` and `Answering:MaxOutputTokens` together.

An answer that still describes its own sources ("the excerpts mention...") despite the system prompt's rule
against it counts as `prompter_events_total{source="anthropic",reason="meta_talk"}` and logs a warning. The
offending sentence is removed deterministically before the answer is delivered, so a rising count is a prompt-
adherence signal to investigate, not a user-facing defect by itself.

## Offline checks

```bash
node --test Monitoring/specs/*.spec.mjs
node --check .github/scripts/notify-reindex-failure.mjs
```

These validate artifact structure, paused activation, and mocked notification delivery, not live Grafana
or PromQL evaluation. They do not replace the nonproduction operator acceptance required before activation.
