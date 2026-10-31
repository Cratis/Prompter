#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# shellcheck source=Deployment/scripts/postgres-backup-common.sh
source "$(dirname -- "${BASH_SOURCE[0]}")/postgres-backup-common.sh"
: "${AGE_RECIPIENT:?}" "${PGHOST:?}" "${PGUSER:?}" "${PGDATABASE:?}" "${PGPASSFILE:?}"
check_store
cp "$PGPASSFILE" "$work/pgpass"
chmod 600 "$work/pgpass"
export PGPASSFILE="$work/pgpass"

# The exporting transaction remains alive until both the dump and manifest have used its snapshot.
SNAPSHOT_PID=0
coproc SNAPSHOT { psql -XqAt --set=ON_ERROR_STOP=1; }
snapshot_pid=$SNAPSHOT_PID
snapshot_input=${SNAPSHOT[1]}
snapshot_open=true
printf 'BEGIN ISOLATION LEVEL REPEATABLE READ READ ONLY;\nSELECT pg_export_snapshot();\n' >&"$snapshot_input"
IFS= read -r snapshot <&"${SNAPSHOT[0]}"
[[ $snapshot =~ ^[A-Za-z0-9-]+$ ]] || exit 1
pg_dump --format=custom --no-owner --no-privileges --snapshot="$snapshot" --file "$work/database.dump"
pg_restore --list "$work/database.dump" > /dev/null
metadata "$snapshot" > "$work/metadata.json"
printf 'ROLLBACK;\n\\q\n' >&"$snapshot_input"
wait "$snapshot_pid"
snapshot_open=false
(cd "$work" && sha256sum database.dump metadata.json > SHA256SUMS)
tar -C "$work" -cf "$work/archive.tar" database.dump metadata.json SHA256SUMS
age --recipient "$AGE_RECIPIENT" --output "$work/archive.age" "$work/archive.tar"
# Plaintext never leaves bounded pod-local memory. No PVC and no key material in the archive.
rm -f "$work/database.dump" "$work/metadata.json" "$work/SHA256SUMS" "$work/archive.tar"
s3 s3 cp "$work/archive.age" "$BACKUP_OBJECT/archive.age" --only-show-errors
s3 s3 cp "$BACKUP_OBJECT/archive.age" "$work/remote.age" --only-show-errors
cmp "$work/archive.age" "$work/remote.age"
cipher_hash=$(sha256sum "$work/archive.age")
jq -n --arg id "$BACKUP_ID" --arg sha256 "${cipher_hash%% *}" '{format:1,id:$id,sha256:$sha256}' > "$work/complete.json"
# A failed upload, encryption, checksum or readback can never publish a completion marker.
s3 s3 cp "$work/complete.json" "$BACKUP_OBJECT/complete.json" --only-show-errors
s3 s3 cp "$BACKUP_OBJECT/complete.json" "$work/remote.json" --only-show-errors
cmp "$work/complete.json" "$work/remote.json"
echo 'Encrypted Postgres backup completed; restore verification still required.'
