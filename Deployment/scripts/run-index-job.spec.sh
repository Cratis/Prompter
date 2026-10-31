#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

script="$(cd "$(dirname "$0")" && pwd)/run-index-job.sh"
export PROMPTER_INDEX_MODEL=voyage-4
export PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS=0.06
export PROMPTER_INDEX_MAX_RUN_DOLLARS=0.10
export PROMPTER_INDEX_MAX_DAILY_DOLLARS=0.50
# Offline specs must not inherit an enabled schedule from the caller.
unset PROMPTER_SCHEDULED_INDEXING_ENABLED

expect_rejection() {
    if "$@" --print-manifest example.invalid/prompter:spec index-spec >/dev/null; then
        echo 'Expected manifest generation to reject the invalid request.' >&2
        exit 1
    fi
}

manifest=$(PROMPTER_INDEX_MODE=incremental "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0].args == ["index"] and .spec.backoffLimit == 0' <<<"$manifest"

expect_rejection env PROMPTER_INDEX_MODE=scheduled "$script"
expect_rejection env PROMPTER_INDEX_MODE=scheduled PROMPTER_SCHEDULED_INDEXING_ENABLED=false "$script"
manifest=$(PROMPTER_INDEX_MODE=scheduled PROMPTER_SCHEDULED_INDEXING_ENABLED=true "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0].args == ["index", "--scheduled"]' <<<"$manifest"
jq -e '.spec.template.spec.containers[0].env |
    any(.[]; .name == "Cratis__Prompter__Ingestion__MaxIncrementalTokens" and .value == "1000000") and
    any(.[]; .name == "Cratis__Prompter__Ingestion__MaxRunDollars" and .value == "0.10")' <<<"$manifest"

manifest=$(PROMPTER_INDEX_MODE=full PROMPTER_INDEX_CONFIRMATION='FULL REINDEX' \
    PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS=0.06 PROMPTER_INDEX_MAX_RUN_DOLLARS=0.10 PROMPTER_INDEX_MAX_DAILY_DOLLARS=0.50 \
    "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0] | .args == ["index", "--full", "--confirm-full-reindex=FULL REINDEX"] and
    any(.env[]; .name == "Cratis__Prompter__Ingestion__MaxDailyDollars" and .value == "0.50")' <<<"$manifest"

expect_rejection env PROMPTER_INDEX_MODE=full PROMPTER_INDEX_CONFIRMATION=wrong "$script"
expect_rejection env PROMPTER_INDEX_MODE=invalid "$script"
expect_rejection env PROMPTER_INDEX_MODE=incremental PROMPTER_INDEX_MAX_RUN_DOLLARS=invalid "$script"

# Fake client only: never read a kubeconfig or contact a cluster.
kubectl() {
    case "$*" in
        'auth can-i create jobs.batch -n prompter') return 0 ;;
        *'get jobs '*) return 0 ;;
        *'get deployment prompter '*) printf '%s' 'example.invalid/prompter:spec' ;;
        'create -f -')
            cat >/dev/null
            echo 'hostile-api-body' >&2
            echo 'hostile-api-body'
            return 42
            ;;
        *) echo 'unexpected fake client operation' >&2; return 99 ;;
    esac
}
# Verify the fake identity before exporting the client to the child process.
kubectl auth can-i create jobs.batch -n prompter
export -f kubectl
set +e
output=$(KUBECONFIG=offline-placeholder GITHUB_RUN_ID=123 GITHUB_RUN_ATTEMPT=1 PROMPTER_INDEX_MODE=incremental "$script" 2>&1)
status=$?
set -e
if [[ "$status" -ne 42 || "$output" != '::error::Could not create the index Job.' ]]; then
    echo 'Expected a fixed create failure category without API output.' >&2
    exit 1
fi
unset -f kubectl

echo 'Index Job manifest specs passed (no cluster calls).'
