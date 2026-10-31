#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

script="$(cd "$(dirname "$0")" && pwd)/run-eval-job.sh"
# Offline specs must not inherit settings from the caller.
unset PROMPTER_EVAL_MAX_COST PROMPTER_EVAL_LIMIT PROMPTER_EVAL_CASE PROMPTER_EVAL_TIMEOUT_SECONDS

secret() {
    printf '{"name":"%s","valueFrom":{"secretKeyRef":{"name":"prompter-secrets","key":"%s"}}}' "$1" "$1"
}
fixture_env() {
    local enabled="$1"
    cat <<JSON
[
  {"name":"Cratis__Prompter__Protection__AnsweringEnabled","value":"$enabled"},
  {"name":"Cratis__Prompter__Protection__IndexingEnabled","value":"true"},
  {"name":"Cratis__Prompter__Protection__GitHubWritesEnabled","value":"true"},
  {"name":"Cratis__Prompter__Protection__DailyUsd","value":"5"},
  {"name":"DOTNET_ENVIRONMENT","value":"Staging"},
  {"name":"Cratis__Prompter__Discord__Token","valueFrom":{"secretKeyRef":{"name":"prompter-secrets","key":"Cratis__Prompter__Discord__Token"}}},
  $(secret Cratis__Prompter__ConnectionString),
  $(secret Cratis__Prompter__Anthropic__ApiKey),
  $(secret Cratis__Prompter__Voyage__ApiKey),
  {"name":"Cratis__Prompter__Ingestion__MaxRunDollars","value":"5"}
]
JSON
}

expect_rejection() {
    local input="$1"
    shift
    if "$@" --print-manifest example.invalid/prompter:spec eval-spec <<<"$input" >/dev/null 2>&1; then
        echo 'Expected manifest generation to reject the invalid request.' >&2
        exit 1
    fi
}

enabled_env="$(fixture_env true)"
manifest=$("$script" --print-manifest example.invalid/prompter:spec eval-spec <<<"$enabled_env")
jq -e '.spec.backoffLimit == 0 and .metadata.labels.app == "prompter-eval" and
    .spec.template.spec.containers[0].args == ["eval","--paid","--set","/app/Eval/golden-questions.yaml","--limit","60","--max-cost","2.00","--out","-"]' <<<"$manifest" >/dev/null
jq -e '.spec.template.spec.containers[0].env |
    (map(.name) | all(.[]; (contains("Discord") or contains("Ingestion")) | not)) and
    any(.[]; .name == "Cratis__Prompter__Protection__IndexingEnabled" and .value == "false") and
    any(.[]; .name == "Cratis__Prompter__Protection__GitHubWritesEnabled" and .value == "false") and
    any(.[]; .name == "DOTNET_ENVIRONMENT" and .value == "Production") and
    any(.[]; .name == "Cratis__Prompter__Protection__AnsweringEnabled" and .value == "true")' <<<"$manifest" >/dev/null

expect_rejection "$(fixture_env false)" "$script"
expect_rejection "$enabled_env" env PROMPTER_EVAL_MAX_COST=5.01 "$script"
expect_rejection "$enabled_env" env PROMPTER_EVAL_LIMIT=61 "$script"
expect_rejection "$enabled_env" env PROMPTER_EVAL_CASE=../x "$script"
expect_rejection "${enabled_env//Cratis__Prompter__Anthropic__ApiKey/Cratis__Prompter__Other}" "$script"

report='{"Results":[{"Id":"arc-001","ExpectsAnswer":true,"RetrievalHit":true,"CitationHit":true,"RefusalCorrect":true,"MustMentionHit":true,"Passed":true,"Cost":0.1,"CitedPages":["https://cratis.io/arc/events.md"],"Reason":null}],"PassRate":1,"RetrievalHitRate":1,"RefusalAccuracy":1,"TotalCost":0.1,"CostCapStopped":false,"ExitCode":0}'
extracted=$(printf 'noise\nEVAL-REPORT %s\nmore noise\n' "$report" | "$script" --extract-report)
jq -e '.ExitCode == 0 and .Results[0].Id == "arc-001"' <<<"$extracted" >/dev/null
if printf 'EVAL-REPORT %s\nEVAL-REPORT %s\n' "$report" "$report" | "$script" --extract-report >/dev/null 2>&1; then
    echo 'Expected duplicate reports to be rejected.' >&2
    exit 1
fi
if printf 'no report\n' | "$script" --extract-report >/dev/null 2>&1; then
    echo 'Expected a missing report to be rejected.' >&2
    exit 1
fi
"$script" --render-markdown <<<"$extracted" | grep -q 'arc-001'

# Fake client only: never read a kubeconfig or contact a cluster.
kubectl() {
    case "$*" in
        'auth can-i create jobs.batch -n prompter') return 0 ;;
        *'get jobs '*) return 0 ;;
        *'get deployment prompter -o jsonpath={.spec.template.spec.containers[0].env}') jq -c . <<<"$FAKE_ENV" ;;
        *'get deployment prompter '*) printf '%s' 'example.invalid/prompter:spec' ;;
        'create -f -') cat >/dev/null ;;
        *'get job '*) printf '1:' ;;
        *' logs '*) printf 'noise\nEVAL-REPORT %s\n' "$FAKE_REPORT" ;;
        *) echo 'unexpected fake client operation' >&2; return 99 ;;
    esac
}
kubectl auth can-i create jobs.batch -n prompter
export -f kubectl
export FAKE_ENV="$enabled_env" FAKE_REPORT="$report"
report_dir="$(mktemp -d)"
trap 'rm -rf "$report_dir"' EXIT
output=$(KUBECONFIG=offline-placeholder PROMPTER_EVAL_REPORT_DIR="$report_dir" GITHUB_RUN_ID=123 "$script")
grep -q 'PASS' <<<"$output"
jq -e '.ExitCode == 0' "$report_dir/report.json" >/dev/null
test -s "$report_dir/report.md"

failing="${report/\"ExitCode\":0/\"ExitCode\":1}"
if FAKE_REPORT="$failing" KUBECONFIG=offline-placeholder PROMPTER_EVAL_REPORT_DIR="$report_dir" "$script" >/dev/null 2>&1; then
    echo 'Expected a failing report to fail the run.' >&2
    exit 1
fi
unset -f kubectl

echo 'Eval Job manifest specs passed (no cluster calls).'
