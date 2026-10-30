# Evaluation isolation

Offline evaluation is synthetic evidence of parsing, deterministic citation/refusal scoring,
report generation and baseline rejection rules, **not measured model quality**. It constructs
no host, database, embedding client, answer client or judge client.

```sh
dotnet build Specs/Prompter.Specs.csproj --configuration Release --no-incremental
dotnet Specs/bin/Release/net10.0/Prompter.Specs.dll
node --test Eval/tests/check-baseline.test.mjs
dotnet Eval/bin/Release/net10.0/Cratis.Prompter.Eval.dll --offline --revision <git-revision> --out Eval/results/offline
node Eval/check-baseline.mjs --synthetic --report Eval/results/offline/report-offline.json --baseline Eval/offline-baseline.json
```

The offline JSON/Markdown outputs are reproducible for a given revision. JSON carries fixture
and corpus hashes, synthetic evidence type, scorer version and canned model identities.
Missing judge scores, errors, missing required samples, invalid metrics and provenance
mismatches fail checks. Exit 0 means complete measurement; exit 1 means incomplete scoring;
exit 2 means invalid invocation or blocked paid execution. Quality regressions are assessed
separately by the baseline checker (exit 1).

## Paid execution is blocked

There is no implicit paid mode. `--paid`, `--index` and the default invocation fail before any
external client is constructed, even with approval and credentials. The only recognized
configuration prefix is `Cratis__PrompterEval__`; production settings, appsettings files,
user-secrets and CLI credential overrides are never loaded. Do not put secrets in command
arguments or reports.

`baseline.json` is explicitly **unmeasured**. Its provisional targets are not regression
floors. Do not replace it with invented numbers or promote offline synthetic scores.

Before enabling paid execution, integrate the aggregate budget/admission boundary for
**indexing, embedding, answering and judging**, with durable usage accounting and fail-closed
reservation. Pin answer/judge/embedding model identities, a golden fixture hash and an
isolated disposable corpus manifest; record a real source revision. Validate disposable
(non-production) storage and cleanup. No live evaluation has been performed here.

The manual `paid-preflight` workflow uses the separate GitHub `eval` environment and only
`EVAL_ANTHROPIC_API_KEY` / `EVAL_VOYAGE_API_KEY`. Repository administrators must configure that
environment with required reviewers and main-only deployment restrictions; YAML cannot
enforce these protection settings. It deliberately fails until the accounting/disposable
corpus integration is reviewed. It has no paid-run or indexing command and uploads no paid
artifacts. Offline artifacts use an explicit report-only allowlist and three-day retention.

## Remaining coverage

Offline fixtures currently exercise parsing, citation/refusal scoring and report completeness.
They do not yet exercise provider-sourced groundedness or product routing. The parent must
wire routing fixtures after the routing slice lands; paid readiness remains blocked. No
shared application host wiring changes are included in this slice.
