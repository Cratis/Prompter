#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Loads the operator credentials for READ-ONLY access to Prompter's deployment from Bitwarden into the
# CURRENT SHELL ONLY. Source it; do not execute it:
#
#   source Deployment/scripts/load-credentials.sh
#   (cd Deployment && pulumi stack output --stack production)
#   kubectl --kubeconfig "$PROMPTER_KUBECONFIG_PATH" -n prompter get pods
#   prompter_credentials_clear
#
# No file ever holds the values. Production `pulumi preview`, `up` and `refresh` run only in CI
# (Deploy - Production), never from a workstation; use this for `pulumi stack output`, `kubectl get` and
# `kubectl logs`.
#
# The values come from one Bitwarden secure note whose notes field is an env file of `export NAME="value"`
# lines. Only the five whitelisted names below are read, and the values are assigned, never evaluated.
# If BW_SESSION is unset it is read from the session file that ~/.local/bin/cratis-bw-unlock writes.
# Overrides, per shell:
#   PROMPTER_BW_ENV_ITEM      name of the secure note (default: Production: Studio Environment (~/.studio-deploy.env))
#   PROMPTER_BW_SESSION_FILE  session file (default: ~/.config/bitwarden/session)
# No `set -x` here: tracing would print the values.

_prompter_sourced=0
if [ -n "${BASH_VERSION:-}" ]; then
  [ "${BASH_SOURCE[0]}" != "$0" ] && _prompter_sourced=1
elif [ -n "${ZSH_VERSION:-}" ]; then
  case "${ZSH_EVAL_CONTEXT:-}" in *:file*) _prompter_sourced=1 ;; esac
fi
if [ "${_prompter_sourced}" != 1 ]; then
  echo "Source this script instead of running it: source Deployment/scripts/load-credentials.sh" >&2
  exit 1
fi

prompter_credentials_clear() {
  unset PULUMI_CONFIG_PASSPHRASE AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY \
    AWS_REQUEST_CHECKSUM_CALCULATION AWS_RESPONSE_CHECKSUM_VALIDATION PROMPTER_KUBECONFIG_PATH
  echo "Prompter credentials cleared from this shell."
}

_prompter_load_credentials() {
  local item="${PROMPTER_BW_ENV_ITEM:-Production: Studio Environment (~/.studio-deploy.env)}"
  local session_file="${PROMPTER_BW_SESSION_FILE:-$HOME/.config/bitwarden/session}"
  local lines line name value

  command -v bw > /dev/null || { echo "bw (Bitwarden CLI) is required." >&2; return 1; }
  command -v jq > /dev/null || { echo "jq is required." >&2; return 1; }

  if [ -z "${BW_SESSION:-}" ]; then
    if [ -r "${session_file}" ]; then
      BW_SESSION="$(cat "${session_file}")"
      export BW_SESSION
    else
      echo "BW_SESSION is unset and ${session_file} does not exist. Run cratis-bw-unlock first." >&2
      return 1
    fi
  fi

  lines="$(bw list items --search "${item}" | jq -r --arg n "${item}" '.[] | select(.name == $n) | .notes' \
    | tr -d '\r' \
    | grep -E '^export (PULUMI_CONFIG_PASSPHRASE|AWS_ACCESS_KEY_ID|AWS_SECRET_ACCESS_KEY|AWS_REQUEST_CHECKSUM_CALCULATION|AWS_RESPONSE_CHECKSUM_VALIDATION)=')" || lines=""
  if [ -z "${lines}" ]; then
    echo "Bitwarden item '${item}' not found, or it has none of the expected export lines." >&2
    return 1
  fi

  while IFS= read -r line; do
    line="${line#export }"
    name="${line%%=*}"
    value="${line#*=}"
    # Strip one pair of surrounding quotes; the value is otherwise taken literally.
    case "${value}" in
      \"*\") value="${value#\"}"; value="${value%\"}" ;;
      \'*\') value="${value#\'}"; value="${value%\'}" ;;
    esac
    export "${name}=${value}"
  done <<< "${lines}"

  if [ -z "${PULUMI_CONFIG_PASSPHRASE:-}" ] || [ -z "${AWS_ACCESS_KEY_ID:-}" ] || [ -z "${AWS_SECRET_ACCESS_KEY:-}" ]; then
    echo "The Bitwarden note is missing the passphrase or the state backend key pair." >&2
    return 1
  fi

  # Pulumi < 3.261.0 against an S3-compatible endpoint needs these (aws-sdk-go-v2 regression).
  export AWS_REQUEST_CHECKSUM_CALCULATION="${AWS_REQUEST_CHECKSUM_CALCULATION:-when_required}"
  export AWS_RESPONSE_CHECKSUM_VALIDATION="${AWS_RESPONSE_CHECKSUM_VALIDATION:-when_required}"

  # Optional: the kubeconfig that generate-prompter-kubeconfig.sh writes is a file on disk by nature.
  local kubeconfig="${PROMPTER_KUBE_DIR:-$HOME/.prompter-kube}/config"
  if [ -r "${kubeconfig}" ]; then
    export PROMPTER_KUBECONFIG_PATH="${kubeconfig}"
  fi

  echo "Set: PULUMI_CONFIG_PASSPHRASE, AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY, AWS_REQUEST_CHECKSUM_CALCULATION, AWS_RESPONSE_CHECKSUM_VALIDATION${PROMPTER_KUBECONFIG_PATH:+, PROMPTER_KUBECONFIG_PATH}."
}

_prompter_load_credentials
_prompter_status=$?
unset -f _prompter_load_credentials
unset _prompter_sourced
return "${_prompter_status}"
