#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# No remote or caller-selected restore target is supported, even with approval.
[[ ${RESTORE_APPROVED:-} == isolated-local-only ]] || exit 1
for name in PGHOST PGHOSTADDR PGPORT PGDATABASE PGUSER PGPASSWORD PGPASSFILE PGSERVICE PGSERVICEFILE PGDATA; do
    [[ -z ${!name:-} ]] || { echo 'Restore refuses inherited database configuration.' >&2; exit 1; }
done
# shellcheck source=Deployment/scripts/postgres-backup-common.sh
source "$(dirname -- "${BASH_SOURCE[0]}")/postgres-backup-common.sh"
: "${AGE_IDENTITY:?}"
s3 s3 cp "$BACKUP_OBJECT/complete.json" "$work/complete.json" --only-show-errors
jq -e --arg id "$BACKUP_ID" '.format == 1 and .id == $id and (.sha256 | test("^[a-f0-9]{64}$"))' "$work/complete.json" >/dev/null
s3 s3 cp "$BACKUP_OBJECT/archive.age" "$work/archive.age" --only-show-errors
expected=$(jq -r '.sha256' "$work/complete.json")
actual=$(sha256sum "$work/archive.age")
[[ ${actual%% *} == "$expected" ]] || exit 1
age --decrypt --identity "$AGE_IDENTITY" --output "$work/archive.tar" "$work/archive.age"
# No archive-controlled paths, links, permissions or extra files are extracted.
for member in database.dump metadata.json SHA256SUMS; do
    tar -xOf "$work/archive.tar" "$member" > "$work/$member"
done
(cd "$work" && sha256sum database.dump metadata.json > ACTUALSUMS && cmp SHA256SUMS ACTUALSUMS)
mkdir "$work/socket"
# mktemp makes a fresh empty target on ephemeral storage; never mounts the production PVC.
initdb -D "$work/data" --username=restore --auth-local=trust --auth-host=reject >/dev/null
server_started=true
pg_ctl -D "$work/data" -l "$work/server.log" -o "-k $work/socket -c listen_addresses='' -c unix_socket_permissions=0700" -w start >/dev/null
export PGHOST="$work/socket" PGUSER=restore PGDATABASE=postgres
pg_restore --exit-on-error --single-transaction --no-owner --no-privileges --dbname=postgres "$work/database.dump" >/dev/null
metadata '' > "$work/restored.json"
cmp "$work/metadata.json" "$work/restored.json"
echo 'Isolated Postgres restore verified; production was not modified.'
