#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

script="$(cd "$(dirname "$0")" && pwd)/run-index-job.sh"

expect_rejection() {
    if "$@" --print-manifest example.invalid/prompter:spec index-spec >/dev/null; then
        echo 'Expected manifest generation to reject the invalid request.' >&2
        exit 1
    fi
}

manifest=$(PROMPTER_INDEX_MODE=incremental "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0].args == ["index"] and .spec.backoffLimit == 0' <<<"$manifest"

manifest=$(PROMPTER_INDEX_MODE=scheduled "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0].args == ["index", "--scheduled"]' <<<"$manifest"

manifest=$(PROMPTER_INDEX_MODE=full PROMPTER_INDEX_CONFIRMATION='FULL REINDEX' \
    PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS=0.5 PROMPTER_INDEX_MAX_RUN_DOLLARS=1 PROMPTER_INDEX_MAX_DAILY_DOLLARS=2 \
    "$script" --print-manifest example.invalid/prompter:spec index-spec)
jq -e '.spec.template.spec.containers[0] | .args == ["index", "--full", "--confirm-full-reindex=FULL REINDEX"] and
    any(.env[]; .name == "Cratis__Prompter__Ingestion__MaxDailyDollars" and .value == "2")' <<<"$manifest"

expect_rejection env PROMPTER_INDEX_MODE=full PROMPTER_INDEX_CONFIRMATION=wrong "$script"
expect_rejection env PROMPTER_INDEX_MODE=invalid "$script"
expect_rejection env PROMPTER_INDEX_MODE=incremental PROMPTER_INDEX_MAX_RUN_DOLLARS=invalid "$script"

echo 'Index Job manifest specs passed (no cluster calls).'
