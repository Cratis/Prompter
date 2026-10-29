#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Sets the `production` environment secrets the Prompter deploy workflows need on Cratis/Prompter, taking
# the values from Bitwarden and from files on disk. Every value goes to `gh secret set` on stdin; none is
# printed, and none is placed on a command line.
#
#   Deployment/scripts/set-github-secrets.sh [--dry-run]
#
#   --dry-run   Resolve every value and list the secret names that would be set, setting nothing.
#
# Bitwarden items read: the note named by PROMPTER_BW_ENV_ITEM (state backend keys and passphrase, see
# load-credentials.sh), "Anthropic - Prompter - API Key" (note), "Voyage AI - Cratis" (login password),
# "Prompter - Reindex Secret" (note, `openssl rand -hex 32`) and "Discord - Prompter - Bot token" (note).
# To rotate the reindex secret, replace the note's value and rerun; the Documentation build's copy must be
# updated at the same time.
#
# Prerequisites: gh (authenticated with access to Cratis/Prompter), bw, jq, an unlocked Bitwarden
# (cratis-bw-unlock), and the kubeconfig from generate-prompter-kubeconfig.sh.
set -euo pipefail
# Never trace: tracing (for example from SHELLOPTS or bash -x) would print the values.
{ set +x; } 2> /dev/null

repo="Cratis/Prompter"
# Production credentials are environment secrets: only jobs in the `production` environment, which only
# runs from main, can read them. Repository secrets would reach every workflow, including pull requests.
environment="production"
dry_run=0
for argument in "$@"; do
  case "${argument}" in
    --dry-run) dry_run=1 ;;
    *) echo "Unknown option: ${argument}" >&2; exit 1 ;;
  esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=/dev/null
source "${script_dir}/load-credentials.sh"

for tool in gh bw jq; do
  command -v "${tool}" > /dev/null || { echo "${tool} is required." >&2; exit 1; }
done

kubeconfig_file="${PROMPTER_KUBE_DIR:-$HOME/.prompter-kube}/config"
if [ ! -r "${kubeconfig_file}" ]; then
  echo "${kubeconfig_file} not found. Run generate-prompter-kubeconfig.sh first." >&2
  exit 1
fi

bw_item() { bw list items --search "$1" | jq -r --arg n "$1" '.[] | select(.name == $n)'; }

anthropic_name="Anthropic - Prompter - API Key"
voyage_name="Voyage AI - Cratis"
anthropic_key="$(bw_item "${anthropic_name}" | jq -r '.notes // empty' | tr -d '\r\n')"
voyage_key="$(bw_item "${voyage_name}" | jq -r '.login.password // empty')"
if [ -z "${anthropic_key}" ]; then echo "Bitwarden item '${anthropic_name}' has no notes." >&2; exit 1; fi
if [ -z "${voyage_key}" ]; then echo "Bitwarden item '${voyage_name}' has no password." >&2; exit 1; fi

# Sets one secret from stdin. The name is reported; the value never is.
set_secret() {
  local name="$1"
  if [ "${dry_run}" = 1 ]; then
    echo "would set ${name}"
    cat > /dev/null
  else
    gh secret set "${name}" --env "${environment}" --repo "${repo}" > /dev/null
    echo "set ${name} (environment ${environment})"
  fi
}

printf '%s' "${AWS_ACCESS_KEY_ID}" | set_secret STATE_BACKEND_ACCESS_KEY_ID
printf '%s' "${AWS_SECRET_ACCESS_KEY}" | set_secret STATE_BACKEND_SECRET_ACCESS_KEY
printf '%s' "${PULUMI_CONFIG_PASSPHRASE}" | set_secret PULUMI_CONFIG_PASSPHRASE
printf '%s' "${anthropic_key}" | set_secret ANTHROPIC_API_KEY
printf '%s' "${voyage_key}" | set_secret VOYAGE_API_KEY
set_secret KUBECONFIG < "${kubeconfig_file}"

reindex_name="Prompter - Reindex Secret"
reindex_secret="$(bw_item "${reindex_name}" | jq -r '.notes // empty' | tr -d '\r\n')"
if [ -n "${reindex_secret}" ]; then
  printf '%s' "${reindex_secret}" | set_secret REINDEX_SECRET
else
  echo "skipped REINDEX_SECRET (Bitwarden note '${reindex_name}' not found)"
fi

discord_name="Discord - Prompter - Bot token"
discord_token="$(bw_item "${discord_name}" | jq -r '.notes // empty' | tr -d '\r\n ')"
if [ -n "${discord_token}" ]; then
  printf '%s' "${discord_token}" | set_secret DISCORD_TOKEN
else
  echo "skipped DISCORD_TOKEN (Bitwarden note '${discord_name}' not found)"
fi
unset anthropic_key voyage_key reindex_secret discord_token
