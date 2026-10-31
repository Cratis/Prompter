#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

set -euo pipefail
(( BASH_VERSINFO[0] >= 5 )) || { echo 'Bash 5 or later required.' >&2; exit 1; }
umask 077
[[ ${BACKUPS_ENABLED:-} == true ]] || { echo 'Postgres backups are disabled.' >&2; exit 1; }
: "${BACKUP_BUCKET:?}" "${BACKUP_ENDPOINT:?}" "${BACKUP_ID:?}"
[[ $BACKUP_ENDPOINT == https://* && $BACKUP_BUCKET =~ ^[a-z0-9][a-z0-9.-]+$ && $BACKUP_ID =~ ^[0-9]{8}T[0-9]{6}Z$ ]] || exit 1
export AWS_MAX_ATTEMPTS=3 AWS_RETRY_MODE=standard AWS_EC2_METADATA_DISABLED=true AWS_PAGER=''
export PGCONNECT_TIMEOUT=15 PGOPTIONS='-c statement_timeout=1800000 -c lock_timeout=10000'
work=$(mktemp -d)
server_started=false
snapshot_open=false
snapshot_input=0
snapshot_pid=0
cleanup() {
    local result=$?
    trap - EXIT
    if [[ $snapshot_open == true ]]; then
        printf 'ROLLBACK;\n\\q\n' >&"$snapshot_input" || true
        wait "$snapshot_pid" || true
    fi
    if [[ $server_started == true ]]; then pg_ctl -D "$work/data" -m immediate -w stop >/dev/null 2>&1 || true; fi
    rm -rf -- "$work"
    exit "$result"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
s3() { aws --endpoint-url "$BACKUP_ENDPOINT" --cli-connect-timeout 15 --cli-read-timeout 120 "$@"; }
export BACKUP_OBJECT="s3://$BACKUP_BUCKET/prompter/$BACKUP_ID"
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)

check_store() {
    s3 s3api get-bucket-versioning --bucket "$BACKUP_BUCKET" > "$work/versioning.json"
    jq -e '.Status == null' "$work/versioning.json" >/dev/null
    s3 s3api get-bucket-lifecycle-configuration --bucket "$BACKUP_BUCKET" > "$work/lifecycle.json"
    jq -e '[.Rules[] | select(.Status == "Enabled" and (.Filter == {"Prefix":""} or .Filter == {} or .Prefix == "") and .Expiration.Days == 14 and .AbortIncompleteMultipartUpload.DaysAfterInitiation <= 1)] | length > 0' "$work/lifecycle.json" >/dev/null
    if s3 s3api get-object-lock-configuration --bucket "$BACKUP_BUCKET" > "$work/lock.json" 2> "$work/lock-error"; then
        jq -e '.ObjectLockConfiguration.ObjectLockEnabled == null' "$work/lock.json" >/dev/null
    else
        grep -q '(ObjectLockConfigurationNotFoundError)' "$work/lock-error" || return 1
    fi
}
metadata() {
    psql -XqAt --set=ON_ERROR_STOP=1 --set="snapshot=$1" --file="$script_dir/postgres-backup-metadata.sql"
}
