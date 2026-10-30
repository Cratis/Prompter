---
title: Monitor Prompter
description: Private aggregate metrics, dashboards, reindex failure delivery, and operator runbooks.
---

# Monitor Prompter

Prompter exports **only aggregate metrics** through Core's private OpenTelemetry collector to Prometheus.
There is no `/metrics` listener, public scrape route, push gateway, automatic HTTP instrumentation, or
GenAI prompt/completion capture. Labels are fixed allowlists: provider, model (unrecognized models become
`other`), task, logical account, operation, outcome, token kind, and usage knowledge. No message text,
user/channel identifiers, operation UUIDs, URLs, exception text, credentials, or provider response bodies
are recorded. Existing logs and interaction retention are separate from this export.

## Enable private metrics

Exporting defaults **off**. Set `Cratis__Prompter__Metrics__Enabled=true` for a standalone process, or
`prompter-deployment:metricsEnabled: true` in the deployment's nonsecret stack configuration before the
normal authorized deployment. Set it to `false` to disable exporting. The deployment forwards its configured
`environment` to `Cratis__Prompter__Metrics__Environment`; standalone processes can set that option directly.
The OTLP resource includes `service.name=prompter` and `deployment.environment.name`, allowing only
`production`, `staging`, `development`, or `test`; unset or unrecognized values become `unknown`.
These are deployment configuration values, never request or user labels. No other OTEL environment variables
are required: the only destination is
`http://otel-collector.monitoring.svc.cluster.local:4318/v1/metrics` (OTLP HTTP/protobuf).
The deployment adds egress only to collector pods in `monitoring` on TCP 4318; Core still owns DNS,
namespace policies, collector ingress and Prometheus scraping. Confirm those policies permit Prompter
before activation. No public ports or ingress are added.

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

Defaults: telemetry absent/stale for five minutes; failed/cancelled pipeline after latest success for two
minutes; no success in more than 30 hours for 15 minutes; 70% and 90% of a UTC-day exposure ceiling for
five minutes (warning and critical respectively); hard cap reached (100%) with no pending delay.
Spend tiers cover the interactive total, each provider, and the separate indexing ceiling. These are
initial operational thresholds, not measured percentiles. Other defaults:
provider errors above 20% over ten minutes with at least five attempts for five minutes; at least three
429s over ten minutes for two minutes; any auth/quota/exhausted-overload event for one minute; ten
admission/protection denials over ten minutes for five minutes. Review these thresholds against traffic.
Expected empty provider series do not alert. The dedicated missing-telemetry rule handles absent index
snapshots; Grafana datasource execution failures alert rather than silently resolving.

## Scheduled reindex failure delivery

The **Reindex - Production** workflow waits for the one-shot indexing Job's actual completion and fails
on Job failure. A failure/cancellation step optionally sends a generic JSON notification to the GitHub
repository secret `PROMPTER_ALERT_WEBHOOK`. An unset secret prints a disabled notice and succeeds.
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
ledger. `prompter_state_index_usd` reads indexing's retry-inclusive daily reservations. These are
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
`v1_5_0.sql` migration ran. Check `prompter_events_total{reason="telemetry_failure"}`. Never open a public
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
never automatically raise caps in response to an alert.

## Provider errors

HTTP 401/403 is `auth`, 402 is `quota`, 429 is `rate_limit`, and verified Anthropic 503/529 overload is `capacity`.
A suspension without a distinct safe machine signal is included in auth/provider failure; no raw error
text is inspected to guess suspension or account allocation. Follow provider status and account health
through approved tooling. Quota-confirmed secondary fallback is measured separately and does not change
account authorization; capacity retries never authorize fallback. Do not add retries, switch accounts,
expose keys or copy provider response bodies while investigating.

## Offline checks

```bash
node --test Monitoring/specs/*.spec.mjs
node --check .github/scripts/notify-reindex-failure.mjs
```

These validate artifact structure, paused activation, and mocked notification delivery, not live Grafana
or PromQL evaluation. Operator acceptance must confirm translated metric names, query evaluation,
firing/resolution and actual notification receipt in a nonproduction test environment before activation.
