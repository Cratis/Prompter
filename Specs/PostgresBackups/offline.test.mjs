// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, readFileSync, readdirSync, rmSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve, join } from 'node:path';
import { spawnSync } from 'node:child_process';

const scripts = resolve('Deployment/scripts');
// No real provider/database/tool command is reachable: every effectful executable is stubbed.
const stub = `#!/bin/bash
set -eu
command_name=\${0##*/}
printf '%s %s\\n' "$command_name" "$*" >> "$CALLS"
[[ "$command_name" != "$FAIL_COMMAND" ]] || exit 19
case "$command_name" in
  psql) if [[ "$*" == *--file=* ]]; then echo "\${METADATA:-fixture}"; else while IFS= read -r line; do [[ "$line" != *pg_export_snapshot* ]] || echo snapshot; [[ "$line" != *q* ]] || break; done; fi ;;
  pg_dump) while (( $# )); do if [[ "$1" == --file ]]; then shift; printf dump > "$1"; fi; shift; done ;;
  pg_restore) : ;;
  age) decrypt=false; [[ "$*" != *--decrypt* ]] || decrypt=true
       while (( $# )); do if [[ "$1" == --output ]]; then shift; if $decrypt; then cp "$FIXTURE_ARCHIVE" "$1"; else printf encrypted > "$1"; fi; fi; shift; done ;;
  aws) while [[ "$1" != s3 && "$1" != s3api ]]; do shift; done
       if [[ "$*" == *get-bucket-versioning* ]]; then if [[ -n \${STORE_VERSIONING:-} ]]; then echo "$STORE_VERSIONING"; else echo '{}'; fi;
       elif [[ "$*" == *get-bucket-lifecycle-configuration* ]]; then
           if [[ -n \${STORE_LIFECYCLE:-} ]]; then echo "$STORE_LIFECYCLE"; else echo '{"Rules":[{"Status":"Enabled","Filter":{"Prefix":""},"Expiration":{"Days":14},"AbortIncompleteMultipartUpload":{"DaysAfterInitiation":1}}]}'; fi
       elif [[ "$*" == *get-object-lock-configuration* ]]; then
           if [[ -n \${STORE_LOCK:-} ]]; then echo "$STORE_LOCK"; else echo '{}'; fi
       elif [[ "$1 $2" == 's3 cp' ]]; then
           [[ \${FAIL_UPLOAD:-} != true || "$3" == s3://* ]] || exit 19
           if [[ "$3" == s3://* ]]; then cp "$STORE/\${3##*/}" "$4"; else cp "$3" "$STORE/\${4##*/}"; fi
           if [[ \${CORRUPT_UPLOAD:-} == true && "$3" == s3://*/archive.age ]]; then printf corrupt > "$4"; fi
       fi ;;
  kubectl) case "$*" in
      *get\\ configmap*) echo '{"spec":{"template":{"spec":{"containers":[{"env":[]}]}}}}' ;;
      *create*) [[ \${JOB_EXISTS:-} != true ]] || exit 19 ;;
      *get\\ job*) echo "\${JOB_STATE:-Complete}" ;;
  esac ;;
esac
`;

function run(script, fail = '', overrides = {}) {
    const scratch = resolve('.ai-work/prompter-hardening');
    mkdirSync(scratch, { recursive: true });
    const root = mkdtempSync(join(scratch, 'offline-'));
    const bin = join(root, 'bin');
    const work = join(root, 'work');
    mkdirSync(bin); mkdirSync(work);
    for (const tool of ['psql', 'pg_dump', 'pg_restore', 'age', 'aws', 'initdb', 'pg_ctl', 'kubectl']) {
        writeFileSync(join(bin, tool), stub, { mode: 0o700 });
    }
    const store = join(root, 'store');
    mkdirSync(store);
    writeFileSync(join(root, 'database.dump'), 'dump');
    writeFileSync(join(root, 'metadata.json'), 'fixture\n');
    const hash = value => createHash('sha256').update(value).digest('hex');
    writeFileSync(join(root, 'SHA256SUMS'), `${hash('dump')}  database.dump\n${hash('fixture\n')}  metadata.json\n`);
    if (overrides.CORRUPT_SUMS === 'true') writeFileSync(join(root, 'SHA256SUMS'), 'wrong\n');
    const fixture = join(root, 'fixture.tar');
    assert.equal(spawnSync('tar', ['-C', root, '-cf', fixture, 'database.dump', 'metadata.json', 'SHA256SUMS']).status, 0);
    writeFileSync(join(store, 'archive.age'), 'encrypted');
    writeFileSync(join(store, 'complete.json'), JSON.stringify({ format: 1, id: '20261030T010000Z', sha256: hash('encrypted') }));
    if (overrides.CORRUPT_ARCHIVE === 'true') writeFileSync(join(store, 'archive.age'), 'corrupt');
    const calls = join(root, 'calls');
    writeFileSync(calls, '');
    writeFileSync(join(root, 'unused'), 'offline-fixture');
    const result = spawnSync('bash', [join(scripts, script), ...(script === 'run-postgres-job.sh' ? [overrides.MODE ?? 'backup'] : [])], {
        env: { PATH: `${bin}:/opt/homebrew/bin:/usr/bin:/bin:/sbin`, HOME: root, TMPDIR: work,
            BACKUPS_ENABLED: 'true', BACKUP_BUCKET: 'offline-fixture', BACKUP_ENDPOINT: 'https://storage.invalid',
            AGE_RECIPIENT: 'age1offline', BACKUP_ID: '20261030T010000Z',
            ...(script === 'backup-postgres.sh' ? { PGHOST: 'postgres', PGDATABASE: 'prompter', PGUSER: 'backup', PGPASSFILE: join(root, 'unused') } : {}),
            RESTORE_APPROVED: 'isolated-local-only', AGE_IDENTITY: join(root, 'unused'),
            CALLS: calls, FAIL_COMMAND: fail, STORE: store, FIXTURE_ARCHIVE: fixture, ...overrides },
        encoding: 'utf8', timeout: 10000,
    });
    const output = { ...result, calls: readFileSync(calls, 'utf8'), leftovers: readdirSync(work) };
    rmSync(root, { recursive: true, force: true });
    return output;
}

for (const script of ['backup-postgres.sh', 'restore-postgres-test.sh']) {
    test(`${script} refuses activation by default`, () => {
        const result = run(script, '', { BACKUPS_ENABLED: '' });
        assert.notEqual(result.status, 0);
        assert.equal(result.calls, '');
    });
}
for (const failure of ['pg_dump', 'age', 'aws', 'pg_restore', 'psql']) {
    test(`backup fails closed on ${failure} failure`, () => {
        const result = run('backup-postgres.sh', failure);
        assert.notEqual(result.status, 0);
        assert.match(result.calls, new RegExp(`^${failure} `, 'm'));
        assert.doesNotMatch(result.calls, /complete\.json/);
        assert.deepEqual(result.leftovers, []);
    });
}
for (const overrides of [{ FAIL_UPLOAD: 'true' }, { CORRUPT_UPLOAD: 'true' }]) {
    test(`backup refuses completion after ${Object.keys(overrides)[0]}`, () => {
        const result = run('backup-postgres.sh', '', overrides);
        assert.notEqual(result.status, 0);
        assert.match(result.calls, /age --recipient/);
        assert.doesNotMatch(result.calls, /complete\.json/);
        assert.deepEqual(result.leftovers, []);
    });
}
test('backup encrypts before upload and commits only after remote byte comparison', () => {
    const result = run('backup-postgres.sh');
    assert.equal(result.status, 0, result.stderr);
    assert.match(result.calls, /--format=custom/);
    assert.ok(result.calls.indexOf('age ') < result.calls.indexOf('s3 cp'));
    assert.match(result.calls, /complete\.json/);
    assert.deepEqual(result.leftovers, []);
});
test('restore refuses inherited production connection configuration', () => {
    const result = run('restore-postgres-test.sh', '', { PGHOST: 'postgres' });
    assert.notEqual(result.status, 0);
    assert.equal(result.calls, '');
});
test('restore requires separate explicit approval', () => {
    const result = run('restore-postgres-test.sh', '', { PGHOST: '', RESTORE_APPROVED: '' });
    assert.notEqual(result.status, 0);
    assert.equal(result.calls, '');
});
test('restore succeeds only against a new local socket database and removes plaintext', () => {
    const result = run('restore-postgres-test.sh');
    assert.equal(result.status, 0, result.stderr);
    assert.match(result.calls, /listen_addresses=''/);
    assert.match(result.calls, /--single-transaction/);
    assert.match(result.calls, /pg_ctl .* stop/);
    assert.deepEqual(result.leftovers, []);
});
for (const overrides of [{ CORRUPT_ARCHIVE: 'true' }, { CORRUPT_SUMS: 'true' }, { METADATA: 'wrong' }]) {
    test(`restore rejects integrity mismatch ${Object.keys(overrides)[0]}`, () => {
        const result = run('restore-postgres-test.sh', '', overrides);
        assert.notEqual(result.status, 0);
        assert.deepEqual(result.leftovers, []);
    });
}
for (const overrides of [{ STORE_LIFECYCLE: '{}' }, { STORE_LOCK: '{"ObjectLockConfiguration":{"ObjectLockEnabled":"Enabled"}}' }]) {
    test(`backup rejects unsafe store ${Object.keys(overrides)[0]}`, () => {
        const result = run('backup-postgres.sh', '', overrides);
        assert.notEqual(result.status, 0);
        assert.doesNotMatch(result.calls, /pg_dump|psql/);
    });
}
for (const status of ['Enabled', 'Suspended']) {
    test(`backup rejects ${status} bucket versioning before database access`, () => {
        const result = run('backup-postgres.sh', '', { STORE_VERSIONING: JSON.stringify({ Status: status }) });
        assert.notEqual(result.status, 0);
        assert.doesNotMatch(result.calls, /pg_dump|psql/);
    });
}
for (const mode of ['backup', 'restore']) {
    test(`runner creates exactly one ${mode} Job and never deletes existing work`, () => {
        const result = run('run-postgres-job.sh', '', { MODE: mode });
        assert.equal(result.status, 0, result.stderr);
        assert.equal(result.calls.match(/kubectl .* create /g)?.length, 1);
        assert.doesNotMatch(result.calls, /delete|replace|apply/);
    });
}
for (const overrides of [{ JOB_EXISTS: 'true' }, { JOB_STATE: 'Failed' }, { BACKUP_ID: 'invalid', MODE: 'restore' }, { BACKUPS_ENABLED: '' }]) {
    test(`runner fails closed for ${Object.keys(overrides)[0]}`, () => {
        const result = run('run-postgres-job.sh', '', overrides);
        assert.notEqual(result.status, 0);
        assert.doesNotMatch(result.calls, /delete|replace|apply/);
    });
}
for (const failure of ['aws', 'age', 'initdb', 'pg_ctl', 'pg_restore', 'psql']) {
    test(`restore fails closed on ${failure} failure`, () => {
        const result = run('restore-postgres-test.sh', failure, { PGHOST: '' });
        assert.notEqual(result.status, 0);
        assert.match(result.calls, new RegExp(`^${failure} `, 'm'));
        assert.deepEqual(result.leftovers, []);
    });
}
