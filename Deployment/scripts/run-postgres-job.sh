#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail
[[ ${BACKUPS_ENABLED:-} == true ]] || { echo 'Postgres backups are disabled.' >&2; exit 1; }
mode=${1:-}
case "$mode" in
    backup) name=prompter-postgres-backup; template=backup-job ;;
    restore)
        name=prompter-postgres-restore; template=restore-job
        [[ ${BACKUP_ID:-} =~ ^[0-9]{8}T[0-9]{6}Z$ ]] || exit 1
        ;;
    *) echo 'Expected backup or restore.' >&2; exit 1 ;;
esac
# The ConfigMap exists only after explicit Pulumi activation. It contains no credential values.
template_json=$(kubectl -n prompter get configmap prompter-postgres-backups -o "jsonpath={.data.$template\\.json}")
[[ -n $template_json ]] || exit 1
if [[ $mode == restore ]]; then
    template_json=$(jq --arg id "$BACKUP_ID" '.spec.template.spec.containers[0].env += [{name:"BACKUP_ID",value:$id}]' <<< "$template_json")
fi
# Fixed name + atomic create is the distributed nonoverlap gate, including manual runs.
# Never delete/adopt an existing Job: cancellation leaves it running to its own deadline.
kubectl -n prompter create -f - <<< "$template_json"
for (( attempt=0; attempt<370; attempt++ )); do
    state=$(kubectl -n prompter get job "$name" -o 'jsonpath={.status.conditions[?(@.status=="True")].type}')
    case "$state" in
        *Failed*|*FailureTarget*) echo 'Postgres operation failed.' >&2; exit 1 ;;
        *Complete*) echo 'Postgres operation completed.'; exit 0 ;;
    esac
    sleep 10
done
echo 'Postgres operation timed out; inspect status without deleting an active Job.' >&2
exit 1
