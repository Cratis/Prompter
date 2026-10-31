# Postgres backup offline checks

Run from the repository root:

```sh
node --test Specs/PostgresBackups/offline.test.mjs
```

Requires Bash 5+, jq, tar and GNU sha256sum. Every database, encryption and
object-storage executable is replaced by a stub. No database, credential,
network access or real backup/restore is used. Tests exercise failures and
cleanup, not cryptographic correctness or compatibility with real Postgres.
Disposable fixtures live under `.ai-work/prompter-hardening/` and are removed.
The C# `for_PostgresBackupJob` specs cover the inert Kubernetes templates.

A real PG17/pgvector restore, upload, lifecycle expiry and alert delivery remain
operator acceptance checks, never inferred from these offline checks.
