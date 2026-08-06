#!/usr/bin/env bash
#
# Set Prompter's deployment secrets against the self-managed (passphrase) Pulumi backend.
# Only the variables you export are set — everything else is skipped, so it is safe to re-run to add
# or rotate individual secrets. Values land in Pulumi.production.yaml as passphrase-encrypted
# `secure: v1:...` entries, which is what makes that file safe to commit.
#
# Usage:
#   export PULUMI_CONFIG_PASSPHRASE=...   # required — the passphrase for this stack
#   export POSTGRES_PASSWORD=...          # export the secrets you want to set (see below)
#   export DISCORD_TOKEN=...
#   export ANTHROPIC_API_KEY=...
#   export VOYAGE_API_KEY=...
#   export REINDEX_SECRET=...
#   ./scripts/set-secrets.sh
#
# Generating the two secrets that are ours to invent:
#   openssl rand -base64 32     # postgresPassword
#   openssl rand -hex 32        # reindexSecret (also goes into the Documentation repo's webhook call)
#
set -euo pipefail

cd "$(dirname "$0")/.."   # Deployment/
STACK="production"

: "${PULUMI_CONFIG_PASSPHRASE:?set PULUMI_CONFIG_PASSPHRASE first}"

set_secret() {
    local key="$1" value="$2"
    [ -z "$value" ] && return 0
    echo "  set prompter-deployment:${key}"
    pulumi config set --secret --stack "$STACK" "prompter-deployment:${key}" "$value"
}

# envvar -> pulumi config key
set_secret postgresPassword  "${POSTGRES_PASSWORD:-}"
set_secret discordToken      "${DISCORD_TOKEN:-}"
set_secret anthropicApiKey   "${ANTHROPIC_API_KEY:-}"
set_secret voyageApiKey      "${VOYAGE_API_KEY:-}"
set_secret reindexSecret     "${REINDEX_SECRET:-}"

echo "Done. Review Deployment/Pulumi.${STACK}.yaml — secrets are stored as 'secure: v1:...'."
