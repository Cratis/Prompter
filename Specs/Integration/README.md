# Indexing database integration specs

This separate project is outside the offline solution test run. It covers advisory-session contention
and release, durable daily scheduled/dollar reservations, model compatibility, and transactional rollback
of stale deletion/profile publication. Vector-column round-trips additionally require pgvector and are
not covered by these plain-Postgres specs. No model provider is called.

Use only a disposable Postgres 16 on `127.0.0.1:55439`, database/user `postgres`, with local-only trust
authentication. **The specs reset their database tables.** Never use a retained or shared database.
With the `postgres:16-alpine` image already available locally:

```bash
docker run --rm --detach --pull=never --name prompter-indexing-specs \
  --env POSTGRES_HOST_AUTH_METHOD=trust --publish 127.0.0.1:55439:5432 postgres:16-alpine
docker exec prompter-indexing-specs pg_isready -U postgres
dotnet test Specs/Integration/Prompter.Indexing.IntegrationSpecs.csproj --configuration Release
docker stop prompter-indexing-specs
```

Wait for readiness before running the specs. The container has no persistent volume and removes itself
on stop. Normal offline specs remain `dotnet test --configuration Release` from the repository root.
