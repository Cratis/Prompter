---
applyTo: "**/*"
---

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
