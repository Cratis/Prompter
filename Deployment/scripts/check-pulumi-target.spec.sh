#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
script="$here/check-pulumi-target.sh"
workflow="$here/../../.github/workflows/deploy-production.yml"
failures=0

admits() {
    if ! bash "$script" "$1" 2>/dev/null; then
        echo "FAIL: expected admission of [$1]"
        failures=$((failures + 1))
    fi
}
refuses() {
    if bash "$script" "$1" 2>/dev/null; then
        echo "FAIL: expected refusal of [$1]"
        failures=$((failures + 1))
    fi
}

admits ""
admits "urn:pulumi:production::prompter-deployment::kubernetes:apps/v1:Deployment::prompter"
refuses "urn:pulumi:test --config prompter-deployment:prompterImage=other"
refuses "* --config prompter-deployment:prompterImage=other"
refuses "--show-secrets"
refuses "urn:pulumi:a"$'\t'"--yes"
refuses "urn:pulumi:a"$'\n'"--yes"
refuses "prompter"
refuses "urn:pulumi:"

# The workflow must run the check on the dispatch input before the deploy step uses it.
# shellcheck disable=SC2016 # literal workflow text, not an expansion
check_line="$(grep -nF 'bash Deployment/scripts/check-pulumi-target.sh "$TARGET"' "$workflow" | head -n 1 | cut -d: -f1)"
deploy_line="$(grep -n 'name: Deploy Prompter stack' "$workflow" | head -n 1 | cut -d: -f1)"
if [ -z "$check_line" ] || [ -z "$deploy_line" ] || [ "$check_line" -ge "$deploy_line" ]; then
    echo "FAIL: deploy-production.yml must check the target before the deploy step"
    failures=$((failures + 1))
fi
# shellcheck disable=SC2016 # literal workflow text, not an expansion
if ! grep -qF 'TARGET: ${{ inputs.target }}' "$workflow"; then
    echo "FAIL: the target check must read inputs.target"
    failures=$((failures + 1))
fi

if [ "$failures" -ne 0 ]; then
    echo "$failures check-pulumi-target spec(s) failed"
    exit 1
fi
echo "check-pulumi-target specs passed"
