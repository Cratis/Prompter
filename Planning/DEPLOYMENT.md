# Deployment — production runbook

How Prompter runs in production. Implementation order is [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md)
M5. Cost target from the research: ≤ €15/month all-in.

## Topology

One small ARM VPS runs everything via Docker Compose: the bot container (`cratis/prompter`, published by
`publish.yml`) and Postgres+pgvector, sharing a private compose network. The bot exposes only `GET /healthz`
and `POST /reindex` (shared-secret) — put them behind the box firewall allowing 80/443 only if the webhook
needs to be reachable from GitHub; otherwise keep the port closed and use a GitHub-runner-initiated call.

- **Box:** Hetzner CAX11 (2 vCPU ARM, 4 GB) — ~€6.50/mo incl. IPv4. Ubuntu LTS + `docker.io`/compose plugin.
- **Images:** multi-arch (`linux/arm64` included) — already handled by `publish.yml`.

## One-time setup (P-26 first)

1. **GitHub:** create `Cratis/Prompter`, push `main`, add secrets `DOCKER_USERNAME`, `DOCKER_PASSWORD`,
   `PAT_DOCUMENTATION`; run `publish.yml` once (workflow_dispatch, version `0.1.0`) → `cratis/prompter:0.1.0`.
2. **Box:** create CAX11, harden (ssh keys only, ufw default deny incoming + allow ssh), install docker.
3. **Deploy dir** `/opt/prompter/` with `docker-compose.production.yml` (to be authored in M5 — bot +
   postgres, named volume, `restart: unless-stopped`, `env_file: .env`) and `.env`:

   ```env
   Cratis__Prompter__ConnectionString=Host=postgres;Port=5432;Database=prompter;Username=prompter;Password=<generated>
   Cratis__Prompter__Discord__Token=…
   Cratis__Prompter__Discord__AskChannelId=…
   Cratis__Prompter__Discord__HelpForumChannelId=…
   Cratis__Prompter__Anthropic__ApiKey=…
   Cratis__Prompter__Voyage__ApiKey=…
   Cratis__Prompter__Reindex__Secret=<generated>
   ```

4. `docker compose up -d`, then one manual `docker compose exec prompter dotnet Cratis.Prompter.dll index`
   (or trigger `/reindex`) to build the corpus.

## Recurring operations

| Concern | How |
|---|---|
| **Update** | `docker compose pull && docker compose up -d` after a release (manual and deliberate — no auto-update) |
| **Docs freshness** | `/reindex` webhook from the Documentation build (M5.1) + the retention/reindex fallback schedule |
| **Backups** | Nightly `pg_dump` to a second volume/object storage — the corpus is rebuildable from cratis.io, so **interactions** are the only data that matters; losing them is annoying, not fatal |
| **Monitoring** | Free uptime monitor on `/healthz`; `docker logs` for diagnosis; weekly glance at refusal rate + feedback ratio in `interactions` |
| **Secrets rotation** | All secrets live only in `.env` on the box + GitHub secrets; rotate Discord token/API keys by editing `.env` + `docker compose up -d` |
| **Data subject requests** | Delete by hashed user id: `DELETE FROM interactions WHERE user_hash = …` (hash the requester's Discord id with the same scheme — see `Discord/UserHash.cs`) |

## GDPR posture (operational summary of D-8)

EU-owned box (Hetzner, Germany/Finland) holds all stored data; the only external processors are the Anthropic
API (answers; no training on API data) and Voyage (embedding text of public docs — no personal data). User
identifiers are stored hashed, questions/answers purge after `RetentionDays` (90). The privacy notice pinned
in Discord names all of this. If EU-region *inference* ever becomes a requirement (Q-3), swap the
`IChatClient` registration to Claude via Vertex AI (europe-west) or Bedrock (eu-central-1) — config, not code.
