# Prompter

Prompter is the Cratis community's Discord documentation assistant — a RAG bot (C#/.NET 10, NetCord,
Microsoft.Extensions.AI + Anthropic, Postgres/pgvector hybrid retrieval) that answers questions grounded in
the published docs at https://cratis.io, with citations, or refuses honestly.

## Working artifacts

Session handovers, plans, decision logs, and other AI work records are **local-only**: they live in
the gitignored `.ai-work/` folder at the repository root and must never be committed or pushed. See
`.ai/rules/local-work-artifacts.md`. Work milestone-by-milestone; every task ends verified: build,
specs, and the task's own "done when" check.

## Conventions

This repo follows the shared Cratis conventions (Chronicle.Mcp is the structural template). Until the shared
`.ai/` config is propagated in (backlog P-25), the sibling `cli` repo's `.claude/` rules are the reference —
highlights that always apply:

- Every `.cs` file starts with the two-line Cratis copyright header; file-scoped namespaces; `var`; primary
  constructors; records for data; no `Service`/`Manager`/`Handler`/`Exception` postfixes; custom exception
  types only; `[LoggerMessage]` logging in `*Logging.cs` partials; concepts via `ConceptAs<T>`.
- Specs use Cratis.Specifications BDD style: `for_<Type>/when_<behavior>/and_<condition>.cs`,
  `Establish`/`Because`/`[Fact] void should_*`. The word `when` appears only in `when_` folder names.
- American English everywhere.

## Quality gates (all must pass before any task is "done")

```bash
dotnet build --configuration Release   # zero warnings, zero errors (warnings are errors in Release)
dotnet test --configuration Release
```

## Commands

```bash
docker compose up -d                   # local Postgres + pgvector
cd Source
dotnet run -- index                    # ingest cratis.io into the corpus
dotnet run -- ask "<question>"         # answer from the terminal
dotnet run                             # run as the Discord bot
```

Configuration binds to `Cratis:Prompter` (env vars: `Cratis__Prompter__…`) — see the table in `README.md`.
API keys are never committed; locally use environment variables or `Source/appsettings.Development.json`
(git-ignored pattern to be added when first needed).
