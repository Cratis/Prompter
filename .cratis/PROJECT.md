# Prompter — project context

The Cratis community's Discord documentation assistant — a RAG bot (C#/.NET,
NetCord, Microsoft.Extensions.AI + Anthropic, Postgres/pgvector hybrid
retrieval) that answers questions grounded in the published docs at
https://cratis.io, with citations, or refuses honestly. Chronicle.Mcp is the
structural template for this repository.

## Conventions

- Every `.cs` file starts with the two-line Cratis copyright header;
  file-scoped namespaces; `var`; primary constructors; records for data; no
  `Service`/`Manager`/`Handler`/`Exception` postfixes; custom exception types
  only; `[LoggerMessage]` logging in `*Logging.cs` partials; concepts via
  `ConceptAs<T>`.
- Specs use Cratis.Specifications BDD style: `for_<Type>/when_<behavior>/and_<condition>.cs`,
  `Establish`/`Because`/`[Fact] void should_*`. The word `when` appears only
  in `when_` folder names.
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

Configuration binds to `Cratis:Prompter` (env vars: `Cratis__Prompter__…`) —
see the table in `README.md`. API keys are never committed; locally use
environment variables or `Source/appsettings.Development.json`.

## AI-assisted development

This repository uses the Cratis AI contract:

- **`.cratis/ai.json`** records the subscription — `cratis/documentation` plus the `cratis/engineering/csharp` maintainer cell.
- **`.cratis/PROJECT.md`** (this file) is the canonical project context; the root `AGENTS.md`, `CLAUDE.md`, and `GEMINI.md` are minimal bootstraps that point here and do nothing else.
- There is **no local AI corpus and no generated tool adapters** in this repository. Shared skills arrive through the Cratis AI marketplace plugins (Claude Code, Codex, GitHub Copilot, Cursor, and Pi are installable today — see the [harness guide](https://www.cratis.io/ai/harnesses/)).

For contributors:

1. Install the Cratis plugin for your harness once (per the harness guide); the subscribed profiles' skills then load automatically when tasks match.
2. General, reusable improvements are proposed in [`Cratis/AI`](https://github.com/Cratis/AI) — never copied into, or synchronized from, this repository.
3. Repository-specific facts and conventions belong in this file; repository-local skills live under `.agents/skills/`.
4. AI session work records (plans, handovers, session notes, scratch analyses) stay in the untracked `.ai-work/` folder and never enter git; a durable follow-up becomes a GitHub issue.
