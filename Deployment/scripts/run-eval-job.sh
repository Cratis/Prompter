#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the deployed Prompter image's paid eval as a one-shot Job, without a public route.
# The Job copies the deployed answer-path configuration (allowlisted, secrets by reference only),
# shares the production spend ledger and never enables indexing or GitHub writes. Only a strictly
# validated, text-free report and fixed Kubernetes status are printed; read raw logs locally.
#
#   Deployment/scripts/run-eval-job.sh
#   <deployed container env JSON> | Deployment/scripts/run-eval-job.sh --print-manifest <image> <job-name>
#   <Job logs> | Deployment/scripts/run-eval-job.sh --extract-report
#   <validated report JSON> | Deployment/scripts/run-eval-job.sh --render-markdown
#
# The helper modes touch no cluster. Default mode requires KUBECONFIG and PROMPTER_EVAL_REPORT_DIR.
# Prerequisites: bash, jq, kubectl (default mode only); namespace-scoped access to prompter.
# Inputs: PROMPTER_EVAL_MAX_COST (default 2.00, at most 5), PROMPTER_EVAL_LIMIT (default 60, at most 60),
# PROMPTER_EVAL_CASE (optional case id), PROMPTER_EVAL_TIMEOUT_SECONDS (default 1800, at most 1800).
set -euo pipefail
# Never trace: tracing (for example from SHELLOPTS or bash -x) could expose credentials.
{ set +x; } 2> /dev/null

max_cost="${PROMPTER_EVAL_MAX_COST-2.00}"
limit="${PROMPTER_EVAL_LIMIT-60}"
case_id="${PROMPTER_EVAL_CASE-}"
timeout_seconds="${PROMPTER_EVAL_TIMEOUT_SECONDS-1800}"

if ! [[ "$max_cost" =~ ^[0-9]{1,2}([.][0-9]{1,2})?$ ]] || ! jq -en --arg cost "$max_cost" \
    '($cost | tonumber) > 0 and ($cost | tonumber) <= 5' >/dev/null; then
    echo '::error::PROMPTER_EVAL_MAX_COST must be a positive decimal of at most 5.00.' >&2
    exit 1
fi
if ! [[ "$limit" =~ ^[1-9][0-9]?$ ]] || (( limit > 60 )); then
    echo '::error::PROMPTER_EVAL_LIMIT must be an integer from 1 to 60.' >&2
    exit 1
fi
if [[ -n "$case_id" ]] && ! [[ "$case_id" =~ ^[a-z]{2,12}-[0-9]{3}$ ]]; then
    echo '::error::PROMPTER_EVAL_CASE must be empty or a case id such as arc-001.' >&2
    exit 1
fi
if ! [[ "$timeout_seconds" =~ ^[1-9][0-9]{0,3}$ ]] || (( timeout_seconds > 1800 )); then
    echo '::error::PROMPTER_EVAL_TIMEOUT_SECONDS must be an integer from 1 to 1800.' >&2
    exit 1
fi

eval_args="$(jq -cn --arg limit "$limit" --arg cost "$max_cost" --arg case_id "$case_id" \
    '["eval", "--paid", "--set", "/app/Eval/golden-questions.yaml", "--limit", $limit, "--max-cost", $cost, "--out", "-"] +
     (if $case_id == "" then [] else ["--case", $case_id] end)')"

if [[ "${1:-}" == '--print-manifest' && $# -eq 3 ]]; then
    if ! [[ "$2" =~ ^[a-zA-Z0-9][a-zA-Z0-9./:@_-]{0,254}$ && "$3" =~ ^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$ ]]; then
        echo '::error::Invalid eval image or Job name.' >&2
        exit 1
    fi
    deployed="$(cat)"
    # Only the deployed answer path is copied; every Discord, GitHub, webhook, routing and metrics value stays out.
    allowed='def allowed: .name | . == "DOTNET_ENVIRONMENT" or . == "Cratis__Prompter__ConnectionString" or
        startswith("Cratis__Prompter__Protection__") or startswith("Cratis__Prompter__Anthropic__") or
        startswith("Cratis__Prompter__Answering__") or startswith("Cratis__Prompter__Voyage__");
        def secret_ref: .valueFrom == {secretKeyRef: {name: "prompter-secrets", key: .name}};'
    if ! jq -e 'type == "array" and all(.[]; type == "object" and (.name | type == "string"))' <<<"$deployed" >/dev/null 2>&1; then
        echo '::error::The deployed environment is malformed.' >&2
        exit 1
    fi
    # Without this check the operator kill switch would be bypassed.
    if ! jq -e 'any(.[]; .name == "Cratis__Prompter__Protection__AnsweringEnabled" and .value == "true" and (has("valueFrom") | not))' <<<"$deployed" >/dev/null; then
        echo '::error::Answering is disabled in production; eval refused.' >&2
        exit 1
    fi
    if ! jq -e "$allowed"'
        [.[] | select(allowed)] as $kept
        | ($kept | all(.[];
            ((has("value") and has("valueFrom")) | not) and
            ((has("value") | not) or (.value | type == "string")) and
            ((has("valueFrom") | not) or secret_ref)))
        and (["Cratis__Prompter__ConnectionString", "Cratis__Prompter__Anthropic__ApiKey", "Cratis__Prompter__Voyage__ApiKey"]
            | all(.[]; . as $required | $kept | any(.[]; .name == $required and has("valueFrom") and secret_ref)))' <<<"$deployed" >/dev/null; then
        echo '::error::The deployed environment lacks required secret references or has an unsafe reference.' >&2
        exit 1
    fi
    env_json="$(jq -c "$allowed"'
        ["Cratis__Prompter__Protection__IndexingEnabled", "Cratis__Prompter__Protection__GitHubWritesEnabled", "DOTNET_ENVIRONMENT"] as $forced
        | [.[] | select(allowed) | select(.name | IN($forced[]) | not)
            | with_entries(select(.key | IN("name", "value", "valueFrom")))]
          + [{name: "Cratis__Prompter__Protection__IndexingEnabled", value: "false"},
             {name: "Cratis__Prompter__Protection__GitHubWritesEnabled", value: "false"},
             {name: "DOTNET_ENVIRONMENT", value: "Production"}]' <<<"$deployed")"
    jq -n --arg image "$2" --arg name "$3" --argjson timeout "$timeout_seconds" \
        --argjson eval_args "$eval_args" --argjson env "$env_json" '{
        apiVersion: "batch/v1",
        kind: "Job",
        metadata: {
            name: $name,
            namespace: "prompter",
            labels: {app: "prompter-eval", environment: "production"}
        },
        spec: {
            backoffLimit: 0,
            activeDeadlineSeconds: $timeout,
            ttlSecondsAfterFinished: 86400,
            template: {
                metadata: {labels: {app: "prompter-eval", environment: "production"}},
                spec: {
                    restartPolicy: "Never",
                    automountServiceAccountToken: false,
                    securityContext: {
                        runAsNonRoot: true,
                        runAsUser: 1654,
                        runAsGroup: 1654,
                        seccompProfile: {type: "RuntimeDefault"}
                    },
                    containers: [{
                        name: "eval",
                        image: $image,
                        imagePullPolicy: "IfNotPresent",
                        args: $eval_args,
                        securityContext: {
                            allowPrivilegeEscalation: false,
                            capabilities: {drop: ["ALL"]}
                        },
                        resources: {
                            requests: {cpu: "100m", memory: "256Mi"},
                            limits: {cpu: "1", memory: "512Mi"}
                        },
                        env: $env
                    }]
                }
            }
        }
    }'
    exit 0
fi

if [[ "${1:-}" == '--extract-report' && $# -eq 1 ]]; then
    logs="$(cat)"
    lines="$(printf '%s\n' "$logs" | grep -E '^EVAL-REPORT \{' || true)"
    # Exactly one report line; nothing from the input is ever echoed on failure.
    if [[ -z "$lines" || "$lines" == *$'\n'* ]]; then
        echo '::error::The eval report failed validation.' >&2
        exit 1
    fi
    validated="$(jq -ce '
        def rate: type == "number" and . >= 0 and . <= 1;
        def exact($want): (keys | sort) == ($want | sort);
        def page: type == "string" and length <= 300 and test("\\Ahttps://[A-Za-z0-9.-]+(/[A-Za-z0-9._~%/#-]*)?\\z");
        def term: type == "string" and test("\\A!?[A-Za-z0-9][A-Za-z0-9 .+#_-]{0,39}\\z");
        def result: type == "object" and
            (exact(["Id", "ExpectsAnswer", "RetrievalHit", "CitationHit", "RefusalCorrect", "MustMentionHit", "Passed", "Cost", "CitedPages", "Reason"]) or
                exact(["Id", "ExpectsAnswer", "RetrievalHit", "CitationHit", "RefusalCorrect", "MustMentionHit", "Passed", "Cost", "CitedPages", "Reason", "TermMisses"])) and
            (.TermMisses == null or (.TermMisses | type == "array" and length <= 10 and all(.[]; term))) and
            (.Id | type == "string" and test("\\A[a-z]{2,12}-[0-9]{3}\\z")) and
            ([.ExpectsAnswer, .RetrievalHit, .CitationHit, .RefusalCorrect, .MustMentionHit, .Passed] | all(.[]; type == "boolean")) and
            (.Cost | type == "number" and . >= 0) and
            (.CitedPages | type == "array" and length <= 20 and all(.[]; page)) and
            (.Reason == null or .Reason == "refused by protection");
        select(type == "object" and
            exact(["Results", "PassRate", "RetrievalHitRate", "RefusalAccuracy", "TotalCost", "CostCapStopped", "ExitCode"]) and
            (.PassRate | rate) and (.RetrievalHitRate | rate) and (.RefusalAccuracy | rate) and
            (.TotalCost | type == "number" and . >= 0 and . <= 10) and
            (.CostCapStopped | type == "boolean") and
            (.ExitCode | type == "number" and (. == 0 or . == 1)) and
            (.Results | type == "array" and length <= 200 and all(.[]; result)))' <<<"${lines#EVAL-REPORT }" 2>/dev/null || true)"
    if [[ -z "$validated" ]]; then
        echo '::error::The eval report failed validation.' >&2
        exit 1
    fi
    printf '%s\n' "$validated"
    exit 0
fi

if [[ "${1:-}" == '--render-markdown' && $# -eq 1 ]]; then
    jq -r '
        def pct: ((. * 1000 | round) / 10 | tostring) + "%";
        def yn: if . then "yes" else "no" end;
        "## Eval - Production",
        "",
        "| Cases | Pass | Retrieval | Refusal | Cost | Cost cap stopped | Result |",
        "| --- | --- | --- | --- | --- | --- | --- |",
        "| \(.Results | length) | \(.PassRate | pct) | \(.RetrievalHitRate | pct) | \(.RefusalAccuracy | pct) | $\((.TotalCost * 10000 | round) / 10000) | \(.CostCapStopped | yn) | \(if .ExitCode == 0 then "PASS" else "FAIL" end) |",
        "",
        "| Case | Passed | Retrieval | Citation | Refusal | Must mention | Term misses | Cost |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
        (.Results[] | "| \(.Id) | \(.Passed | yn) | \(.RetrievalHit | yn) | \(.CitationHit | yn) | \(.RefusalCorrect | yn) | \(.MustMentionHit | yn) | \((.TermMisses // []) | join(", ")) | $\((.Cost * 10000 | round) / 10000) |")'
    exit 0
fi

if [[ $# -ne 0 ]]; then
    echo "Usage: $0 [--print-manifest <image> <job-name> | --extract-report | --render-markdown]" >&2
    exit 1
fi
: "${KUBECONFIG:?KUBECONFIG must be set}"
report_dir="${PROMPTER_EVAL_REPORT_DIR:?PROMPTER_EVAL_REPORT_DIR must be set}"

if ! kubectl auth can-i create jobs.batch -n prompter; then
    echo '::error::The deploy identity cannot create Jobs in prompter.' >&2
    exit 1
fi

for app in prompter-eval prompter-index; do
    active_jobs="$(kubectl -n prompter get jobs -l "app=$app" -o jsonpath='{range .items[?(@.status.active)]}{.metadata.name}{"\n"}{end}')"
    if [[ -n "$active_jobs" ]]; then
        echo "::error::A production Job is already active: $active_jobs" >&2
        exit 1
    fi
done

image="$(kubectl -n prompter get deployment prompter -o jsonpath='{.spec.template.spec.containers[0].image}')"
if [[ -z "$image" ]]; then
    echo '::error::The deployed Prompter image is empty.' >&2
    exit 1
fi
# Project only the env list: never print the whole Deployment (secrets rule).
deployed_env="$(kubectl -n prompter get deployment prompter -o jsonpath='{.spec.template.spec.containers[0].env}')"
job_name="prompter-eval-${GITHUB_RUN_ID:-manual-$(date +%s)}-${GITHUB_RUN_ATTEMPT:-1}"
# Validate before invoking create: an invalid manifest must never reach the API.
manifest="$("$0" --print-manifest "$image" "$job_name" <<<"$deployed_env")"
create_exit=0
kubectl create -f - <<<"$manifest" >/dev/null 2>&1 || create_exit=$?
if [[ $create_exit -ne 0 ]]; then
    echo '::error::Could not create the eval Job.' >&2
    exit "$create_exit"
fi

finished=false
job_succeeded=false
wait_deadline=$((SECONDS + timeout_seconds + 60))
while (( SECONDS < wait_deadline )); do
    status="$(kubectl -n prompter get job "$job_name" -o jsonpath='{.status.succeeded}{":"}{.status.failed}')"
    case "$status" in
        1:*) finished=true; job_succeeded=true; break ;;
        *:1) finished=true; break ;;
    esac
    sleep 10
done

if [[ "$finished" != 'true' ]]; then
    echo "::error::Eval Job $job_name exceeded the wait deadline." >&2
    echo "Read logs locally with load-credentials.sh: kubectl -n prompter logs job/$job_name" >&2
    exit 1
fi

# Eval exits 1 on a threshold miss, so a failed Job can still carry a report. Raw logs are never printed.
logs="$(kubectl -n prompter logs "job/$job_name" --limit-bytes=4194304 || true)"
if report="$("$0" --extract-report <<<"$logs" 2>/dev/null)"; then
    mkdir -p "$report_dir"
    printf '%s\n' "$report" > "$report_dir/report.json"
    markdown="$("$0" --render-markdown <<<"$report")"
    printf '%s\n' "$markdown" > "$report_dir/report.md"
    printf '%s\n' "$markdown"
    if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
        printf '%s\n' "$markdown" >> "$GITHUB_STEP_SUMMARY"
    fi
    if [[ "$job_succeeded" == 'true' ]] && jq -e '.ExitCode == 0' <<<"$report" >/dev/null; then
        exit 0
    fi
    echo '::error::Eval scores fell below the thresholds or the cost cap stopped the run.' >&2
    exit 1
fi

echo '::error::The eval Job produced no valid report.' >&2
kubectl -n prompter get pods -l "job-name=$job_name" -o jsonpath='{range .items[*]}{range .status.containerStatuses[*]}{.name}{": exitCode="}{.state.terminated.exitCode}{" reason="}{.state.terminated.reason}{"\n"}{end}{end}'
echo "Read logs locally with load-credentials.sh: kubectl -n prompter logs job/$job_name" >&2
exit 1
