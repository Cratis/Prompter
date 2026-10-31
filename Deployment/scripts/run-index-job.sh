#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Runs the deployed Prompter image's index command as a one-shot Job, without a public route.
# Only the fixed index summary and Kubernetes status are printed; read raw logs locally.
#
#   Deployment/scripts/run-index-job.sh
#   Deployment/scripts/run-index-job.sh --print-manifest <image> <job-name>
#
# --print-manifest prints JSON without touching a cluster. Default mode requires KUBECONFIG.
# Prerequisites: bash, jq, kubectl (default mode only); namespace-scoped access to prompter.
# PROMPTER_INDEX_TIMEOUT_SECONDS defaults to 1500; finished Jobs retain logs for 24 hours.
set -euo pipefail
# Never trace: tracing (for example from SHELLOPTS or bash -x) could expose credentials.
{ set +x; } 2> /dev/null

mode="${PROMPTER_INDEX_MODE:-incremental}"
confirmation="${PROMPTER_INDEX_CONFIRMATION:-}"
case "$mode" in
    incremental) index_args='["index"]' ;;
    scheduled)
        if [[ "${PROMPTER_SCHEDULED_INDEXING_ENABLED:-}" != 'true' ]]; then
            echo '::error::Scheduled indexing is disabled.' >&2
            exit 1
        fi
        index_args='["index", "--scheduled"]'
        ;;
    full)
        if [[ "$confirmation" != 'FULL REINDEX' ]]; then
            echo '::error::Full indexing requires the literal confirmation FULL REINDEX.' >&2
            exit 1
        fi
        index_args='["index", "--full", "--confirm-full-reindex=FULL REINDEX"]'
        ;;
    *) echo '::error::Unknown index mode.' >&2; exit 1 ;;
esac

price="${PROMPTER_INDEX_PRICE_PER_MILLION_TOKENS:-0}"
run_dollars="${PROMPTER_INDEX_MAX_RUN_DOLLARS:-0}"
daily_dollars="${PROMPTER_INDEX_MAX_DAILY_DOLLARS:-0}"
for value in "$price" "$run_dollars" "$daily_dollars"; do
    if ! [[ "$value" =~ ^[0-9]{1,2}([.][0-9]{1,6})?$ ]]; then
        echo '::error::Index pricing and dollar limits must be bounded positive decimals.' >&2
        exit 1
    fi
done
if [[ "${PROMPTER_INDEX_MODEL:-}" != 'voyage-4' ]] || ! jq -en \
    --arg price "$price" --arg run "$run_dollars" --arg day "$daily_dollars" \
    '($price | tonumber) == 0.06 and ($run | tonumber) > 0 and
     ($run | tonumber) <= ($day | tonumber) and ($day | tonumber) <= 5' >/dev/null; then
    echo '::error::Unsupported index model, price or budget ordering.' >&2
    exit 1
fi

timeout_seconds="${PROMPTER_INDEX_TIMEOUT_SECONDS:-1500}"
if ! [[ "$timeout_seconds" =~ ^[1-9][0-9]{0,3}$ ]] || (( timeout_seconds > 1500 )); then
    echo '::error::PROMPTER_INDEX_TIMEOUT_SECONDS must be an integer from 1 to 1500.' >&2
    exit 1
fi

if [[ "${1:-}" == '--print-manifest' && $# -eq 3 ]]; then
    if ! [[ "$2" =~ ^[a-zA-Z0-9][a-zA-Z0-9./:@_-]{0,254}$ && "$3" =~ ^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?$ ]]; then
        echo '::error::Invalid index image or Job name.' >&2
        exit 1
    fi
    jq -n --arg image "$2" --arg name "$3" --argjson timeout "$timeout_seconds" \
        --argjson index_args "$index_args" --arg price "$price" --arg run_dollars "$run_dollars" --arg daily_dollars "$daily_dollars" '{
        apiVersion: "batch/v1",
        kind: "Job",
        metadata: {
            name: $name,
            namespace: "prompter",
            labels: {app: "prompter-index", environment: "production"}
        },
        spec: {
            backoffLimit: 0,
            activeDeadlineSeconds: $timeout,
            ttlSecondsAfterFinished: 86400,
            template: {
                metadata: {labels: {app: "prompter-index", environment: "production"}},
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
                        name: "index",
                        image: $image,
                        imagePullPolicy: "IfNotPresent",
                        args: $index_args,
                        securityContext: {
                            allowPrivilegeEscalation: false,
                            capabilities: {drop: ["ALL"]}
                        },
                        resources: {
                            requests: {cpu: "100m", memory: "256Mi"},
                            limits: {cpu: "1", memory: "512Mi"}
                        },
                        env: [
                            {name: "Cratis__Prompter__Protection__IndexingEnabled", value: "true"},
                            {name: "Cratis__Prompter__Protection__AnsweringEnabled", value: "false"},
                            {name: "Cratis__Prompter__Protection__GitHubWritesEnabled", value: "false"},
                            {name: "Cratis__Prompter__Protection__DailyUsd", value: "5"},
                            {name: "Cratis__Prompter__Protection__Voyage__DailyUsd", value: "5"},
                            {name: "Cratis__Prompter__Protection__Voyage__Model", value: "voyage-4"},
                            {name: "Cratis__Prompter__Protection__Voyage__InputUsdPerMillion", value: "0.06"},
                            {name: "Cratis__Prompter__Protection__Voyage__OutputUsdPerMillion", value: "0"},
                            {name: "Cratis__Prompter__Protection__Voyage__MaxInputTokens", value: "32000"},
                            {name: "Cratis__Prompter__Voyage__Model", value: "voyage-4"},
                            {name: "Cratis__Prompter__Voyage__Dimensions", value: "1024"},
                            {name: "Cratis__Prompter__Voyage__BatchSize", value: "128"},
                            {name: "Cratis__Prompter__Voyage__MaxRetries", value: "3"},
                            {name: "Cratis__Prompter__Voyage__IndexMaxRetries", value: "5"},
                            {name: "Cratis__Prompter__Voyage__RetryBaseDelayMilliseconds", value: "500"},
                            {name: "Cratis__Prompter__Voyage__TimeoutSeconds", value: "60"},
                            {name: "Cratis__Prompter__Voyage__IndexTimeoutSeconds", value: "120"},
                            {name: "Cratis__Prompter__Ingestion__MaxIncrementalTokens", value: "1000000"},
                            {name: "Cratis__Prompter__Ingestion__PricePerMillionTokens", value: $price},
                            {name: "Cratis__Prompter__Ingestion__MaxRunDollars", value: $run_dollars},
                            {name: "Cratis__Prompter__Ingestion__MaxDailyDollars", value: $daily_dollars},
                            {name: "DOTNET_ENVIRONMENT", value: "Production"},
                            {
                                name: "Cratis__Prompter__ConnectionString",
                                valueFrom: {secretKeyRef: {
                                    name: "prompter-secrets", key: "Cratis__Prompter__ConnectionString"
                                }}
                            },
                            {
                                name: "Cratis__Prompter__Voyage__ApiKey",
                                valueFrom: {secretKeyRef: {
                                    name: "prompter-secrets", key: "Cratis__Prompter__Voyage__ApiKey"
                                }}
                            }
                        ]
                    }]
                }
            }
        }
    }'
    exit 0
fi

if [[ $# -ne 0 ]]; then
    echo "Usage: $0 [--print-manifest <image> <job-name>]" >&2
    exit 1
fi
: "${KUBECONFIG:?KUBECONFIG must be set}"

if ! kubectl auth can-i create jobs.batch -n prompter; then
    echo '::error::The deploy identity cannot create Jobs in prompter.' >&2
    exit 1
fi

active_jobs="$(kubectl -n prompter get jobs -l app=prompter-index -o jsonpath='{range .items[?(@.status.active)]}{.metadata.name}{"\n"}{end}')"
if [[ -n "$active_jobs" ]]; then
    echo "::error::An index Job is already active: $active_jobs" >&2
    exit 1
fi

image="$(kubectl -n prompter get deployment prompter -o jsonpath='{.spec.template.spec.containers[0].image}')"
if [[ -z "$image" ]]; then
    echo '::error::The deployed Prompter image is empty.' >&2
    exit 1
fi
job_name="prompter-index-${GITHUB_RUN_ID:-manual-$(date +%s)}-${GITHUB_RUN_ATTEMPT:-1}"
# Validate before invoking create: an invalid manifest must never reach the API.
manifest="$("$0" --print-manifest "$image" "$job_name")"
create_exit=0
kubectl create -f - <<<"$manifest" >/dev/null 2>&1 || create_exit=$?
if [[ $create_exit -ne 0 ]]; then
    echo '::error::Could not create the index Job.' >&2
    exit "$create_exit"
fi

wait_deadline=$((SECONDS + timeout_seconds + 60))
while (( SECONDS < wait_deadline )); do
    status="$(kubectl -n prompter get job "$job_name" -o jsonpath='{.status.succeeded}{":"}{.status.failed}')"
    case "$status" in
        1:*)
            # Filter in the pipe: raw logs must never reach public Actions output.
            summary="$(kubectl -n prompter logs "job/$job_name" --tail=20 | grep -E '^Indexed [0-9]+ pages in [0-9]{2}:[0-9]{2}: [0-9]+ embedded, [0-9]+ unchanged, [0-9]+ removed\.$')" || summary=''
            if [[ -z "$summary" || "$summary" == 'Indexed 0 pages '* ]]; then
                echo '::error::The index Job succeeded without a non-empty crawl summary.' >&2
                echo "Read logs locally: kubectl -n prompter logs job/$job_name" >&2
                exit 1
            fi
            printf '%s\n' "$summary"
            if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
                printf '%s\n' "$summary" >> "$GITHUB_STEP_SUMMARY"
            fi
            exit 0
            ;;
        *:1) break ;;
    esac
    sleep 10
done

echo "::error::Index Job $job_name failed or exceeded the wait deadline." >&2
kubectl -n prompter get pods -l "job-name=$job_name" -o jsonpath='{range .items[*]}{range .status.containerStatuses[*]}{.name}{": exitCode="}{.state.terminated.exitCode}{" reason="}{.state.terminated.reason}{"\n"}{end}{end}'
echo "Read logs locally with load-credentials.sh: kubectl -n prompter logs job/$job_name" >&2
exit 1
