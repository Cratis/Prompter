# Encrypted Postgres backups

Backups are **disabled by default**. Deployment creates no backup resources unless
`postgresBackupsEnabled` is true; Actions additionally requires the repository
variable `POSTGRES_BACKUPS_ENABLED=true`. Do not enable either gate until the
prerequisites and acceptance checks below are complete. This is a logical backup,
not point-in-time recovery or a database failover mechanism.

## Before activation

1. Apply the privacy/retention changes first, including migration `1.2.0` and any
   required historical-data purge. A backup contains the entire database, not only
   currently searchable documents. Restoring an older archive can resurrect erased
   data: reapply erasures and current retention before considering a production cutover.
2. Choose an object-storage **region and dedicated bucket** with the data owner.
   Keep the bucket private, never versioned (not even suspended), without Object
   Lock or replication. Install and verify a whole-bucket lifecycle: expiration
   after **14 days**, abort incomplete multipart uploads after **one day**. Expiry
   is asynchronous: obtain the provider's deletion/erasure guarantees. Do not
   activate if legal erasure requires shorter retention; destroy affected objects
   and escalate to the data owner rather than treating expiry as immediate deletion.
3. Create a public age recipient. Keep the private identity with an **offline key
   custodian**, separately from storage credentials. Exercise key recovery before
   activation. Rotations must retain old identities for surviving archives, then
   destroy retired identities according to the erasure policy.
4. Approve a digest-pinned Linux tooling image containing Bash 5+, PostgreSQL **17**
   client **and server**, the production pgvector extension version, age, AWS CLI,
   jq, GNU coreutils (`timeout`, `sha256sum`, `cmp`), tar and CA certificates.
   The image must declare a numeric nonroot `USER` with a passwd entry; verify
   `initdb`, mounted Secret readability and writable `/work` offline in that image.
   No existing Postgres UID, pod security context or namespace PodSecurity label
   is changed. The new Jobs require nonroot, drop capabilities, a readonly root
   and no service-account token. Do not relax these controls to fix an image.
5. Ensure the corpus fits: `/work` is a **2 GiB memory-backed emptyDir**, container
   memory limit **4 GiB**, CPU limit **2**. Dump, tar, encrypted archive, readback
   and restored data need simultaneous space. Large corpora fail closed; measure
   this budget before activation. No PVC, Actions artifact or persistent plaintext
   backup directory is used.

## Operator-managed identities

Provision through the approved secret mechanism; never commit values, put them in
Pulumi config, command arguments or workflow inputs, or print their contents.
In the `prompter` namespace, supply:

| Secret | Required keys and authority |
| --- | --- |
| `prompter-postgres-backup-reader` | `pgpass`: libpq passfile for host `postgres`, database `prompter`, role `prompter_backup`. Dedicated LOGIN with CONNECT, schema USAGE, SELECT on existing/future public tables and sequences, no writes or superuser. Test `pg_dump` and exported snapshots with this role. |
| `prompter-backup-store-writer` | `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_DEFAULT_REGION`. Dedicated bucket/prefix identity: PutObject/GetObject on `prompter/*`, multipart initiation/upload/completion/abort, plus GetBucketVersioning/GetLifecycleConfiguration/GetObjectLockConfiguration for preflight. No ListAllMyBuckets, policy changes, deletion or other buckets. |
| `prompter-backup-store-reader` | Same AWS key names, **GetObject only** on `prompter/*`. No source database credentials. |
| `prompter-backup-identity` | `key.txt`: private age identity, provisioned **only for an approved restore drill**, removed afterward by its custodian. Never mounted by the backup Job. |

Grant list/multipart permissions only if required by the selected S3 provider;
use bucket/prefix scope wherever supported. Verify permissions positively and
negatively. Partial uploads have no completion marker and expire by lifecycle.
Preflight rejects versioning and Object Lock and requires the configured lifecycle.
An unsupported or denied preflight API fails closed, including unknown lock errors.
The accepted absence code is `ObjectLockConfigurationNotFoundError`.

## Configure and enable deliberately

Set these **public** Pulumi configuration keys in the existing stack:

| Key | Value |
| --- | --- |
| `postgresBackupImage` | Approved image reference ending in `@sha256:` and 64 hex digits |
| `postgresBackupEndpoint` | HTTPS S3 endpoint, default port 443, no embedded credentials |
| `postgresBackupStoreCidr` | Approved public IPv4 **/32** for that endpoint |
| `postgresBackupBucket` | Dedicated bucket name |
| `postgresBackupRecipient` | Public `age1…` recipient |
| `postgresBackupsEnabled` | `true` only after the prerequisites above |

The scoped NetworkPolicy grants TCP/443 only to that address for the backup/restore
labels; it does not widen the bot policy. Infrastructure's additive baseline still
permits DNS and namespace/private traffic. An isolated restore **database** is
created by construction, not by claiming these additive policies deny every other
network path. Verify endpoint DNS/address changes and cluster policy before use.

Preview and apply using the normal authorized deployment process. This creates
only a ConfigMap containing scripts/inert Job templates and the scoped policy:
**no CronJob, new RBAC or running backup**. Reuse the existing Jobs/ConfigMaps deploy
identity (Infrastructure `Core/Access/AppAccess.cs`); do not add CronJob privileges.

Configure `production` and `postgres-restore-verification` GitHub environments with
main-only branches, appropriate KUBECONFIG secrets and **required human reviewers
for restore**. The workflow names an environment; it cannot configure its protection
rules. Set repository variable `POSTGRES_BACKUPS_ENABLED=true` last. Scheduled
backups then run daily at **04:21 UTC**, or dispatch operation `backup` on main.
Dispatch `restore` with the explicit UTC archive id, e.g. `20261030T010000Z`, only
after custodian approval and temporary identity provisioning.

Both operations share non-canceling workflow concurrency. Atomic creation of a
fixed Job name prevents overlapping same-operation runs outside Actions too.
Jobs have no retries, a 3600-second deadline and a one-hour terminal TTL. The shell
has a shorter 3300-second timeout with a TERM/KILL grace. Never delete an active
Job to make room; cancellation does not imply it stopped. A duplicate dispatch
fails until the prior Job expires. Concurrent backup and restore use distinct
local workspaces and targets; workflow dispatch serializes both.

## Verify recovery before relying on backups

A backup exports one read-only repeatable-read snapshot, uses it for both custom
`pg_dump` and metadata, validates the archive listing, and packages dump checksums
inside the encrypted tar. Only age ciphertext is uploaded. The job downloads and
byte-compares it before publishing `prompter/<id>/complete.json` with format, id and
ciphertext SHA256. A marker is **not proof of recovery**.

An approved restore fetches that marker and exact ciphertext, verifies the hash,
decrypts with the separately mounted identity, and extracts only named files
without archive-controlled paths or links. It compares inner checksums before
starting a new PG17 server in a fresh temporary directory. The server has no TCP
listener, uses a private Unix socket and rejects host authentication. Inherited
Postgres connection settings are rejected: no remote/caller-selected restore target
is supported, no production DSN or PVC is mounted, and no production cutover occurs.

`pg_restore --exit-on-error --single-transaction` must succeed. A deterministic
metadata comparison verifies PostgreSQL major, pgvector version, migration journal,
public column/index definitions, table counts, feedback distribution, 1024 vector
dimensions, vector retrieval and a full-text query. The current schema requires a
nonempty corpus and migration `1.2.0`; a later schema migration must update these
checks deliberately. Cleanup stops the local server and removes plaintext on
success, failure or TERM; pod-local memory also disappears on pod destruction.
Never print dump contents, interaction data, keys or server logs into Actions.

Have the operator record an authorized drill's archive id, image digest, status,
elapsed time and non-sensitive check results. Verify at least one real PG17/pgvector
backup/restore and negative cases (wrong key, corrupt ciphertext, expired object,
insufficient privileges). Inspect lifecycle deletion and actual data erasure in the
chosen provider. These live checks are **not performed by the offline specs**.

## Alerting, recovery and disablement

Activation also requires a named receiver and agreed runbook. Integrate failed Job
status and **no successful backup in 26 hours** into the Infrastructure monitoring
stack after its receiver is configured. Alert on missing scheduled workflow runs,
not just Job failures; successful backups without a recent successful restore drill
must not be presented as recoverable. Deliver a real failure/staleness test to the
receiver before relying on this feature. This repository does not provision that
shared receiver; GitHub failure notifications alone are not freshness monitoring.

Disable the Actions variable first, then `postgresBackupsEnabled` through the normal
deploy process. Neither stops an active Job or deletes archives/identities. Inspect
status and let its deadline finish; arrange separate authorized deletion/credential
revocation with the custodian. TTL/lifecycle handle terminal Jobs/objects, not Secrets.
Actual production recovery remains a separate, explicitly approved maintenance
operation with current migrations, retention and erased-data reconciliation.

Patterns follow Infrastructure's public-recipient/private-identity separation,
per-store credentials, 14-day lifecycle, one-day multipart cleanup and jobs-only
namespace deployment identity; no Infrastructure checkout is modified.
