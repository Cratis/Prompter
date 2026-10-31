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

## Accounting integration contract

Paid execution is unconditionally blocked in `Program.cs`; `PaidApproved=true` cannot enable
it. `EvalConfiguration.PaidBlockReason` always returns a refusal. No readiness flag is needed
to preserve that stronger block. Do not replace it with approval alone.

An integration must keep `EvalHost.Configuration()` environment-prefix-only and build fresh
options from `Cratis__PrompterEval__` values. Add explicit eval protection/pricing/cap settings;
never merge production `PrompterOptions` or rely on their defaults. Keep corpus storage
separate from the aggregate ledger's persistence identity: a disposable corpus database must
not silently create an independent spend allowance. Ledger access must be explicitly supplied
and authorized without inheriting production credentials.

The parent integration points are:

- `EvaluationRun.Score()` calls `IAnswers.For()` (answer and retrieval embeddings), then
  `EvaluationRun.Judge()`. The judge client enters through the constructor's
  `ChatConfiguration` and `GroundednessEvaluator.EvaluateAsync()`. Both clients must be
  funded before dispatch; a raw judge `IChatClient` is not an acceptable registration.
- `ISpendLedger.Reserve(SpendExposure, CancellationToken)` and
  `Reconcile(Guid, SpendExposure, CancellationToken)` are the existing aggregate ceiling.
  Refusal, storage failure, cancellation and unknown usage must never permit an unfunded
  dispatch or refund ambiguous exposure.
- `PaidWork.BeginInteractive()` provides a `WorkLease`; the caller must `Activate()` it
  in the dispatch execution context and dispose it. **One lease currently permits only one
  chat dispatch.** Answer and judge cannot share one interactive lease, nor can retry
  attempts reuse its consumed chat allowance. The parent must fund each attempt through
  its shared admission integration before enabling a paid runner.
- `BudgetedChat.GetResponseAsync()` requires that active lease and validates its configured
  model, prompt and output bounds. Answer and judge need separately pinned model policies;
  the current single `Protection.Anthropic` policy must not silently substitute the answer
  model for the judge model.
- `IProviderAccounting.Reserve(ProviderDispatch, CancellationToken)` and
  `Complete(ProviderDispatch, UsageDetails?, string)` are the provider-attempt seam.
  `AnthropicAccounts` currently accepts null accounting, and `TaskChatPolicy.TaskName()`
  recognizes only `answer` and `issue-draft`. Paid eval needs mandatory accounting plus
  reviewed fixed eval answer/judge task attribution; arbitrary task strings currently fall
  back to `answer`. Do not assume this seam already supplies judge admission.
- For ingestion, use `Indexer.Run(IndexRequest, CancellationToken)` with `IndexBudget.Preflight`
  and `IIndexLease` limits **and** the shared funded embedding chain:
  `ResilientEmbeddingGenerator(BudgetedEmbeddings(VoyageEmbeddings, ledger, evalOptions), ...)`.
  `BudgetedEmbeddings.GenerateAsync()` reserves through `ISpendLedger` before every retry
  when no interactive lease is active. The index-only limits do not replace the aggregate
  ceiling. SDK retries must not introduce hidden unreserved attempts.

Before removing the hard block, add rejection specs for missing accounting/ledger, missing
prices or bounds, exhausted answer/judge/index funding, cancellation and storage failure.
Require explicit paid approval and verified readiness of **all three** funded paths, with
readiness false by default. Keep the workflow's unconditional paid refusal until those
registrations and rejection specs land together. This slice does not define a second budget
or change shared application registration.

## Remaining coverage

Offline fixtures currently exercise parsing, citation/refusal scoring and report completeness.
They do not yet exercise provider-sourced groundedness or product routing. The parent must
wire routing fixtures after the routing slice lands; paid readiness remains blocked. No
shared application host wiring changes are included in this slice.
