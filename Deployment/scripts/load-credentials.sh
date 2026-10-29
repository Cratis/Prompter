#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Loads the operator credentials for READ-ONLY access to Prompter's deployment from Bitwarden into the
# CURRENT SHELL ONLY. Source it; do not execute it:
#
#   export BW_SESSION="$(bw unlock --raw)"   # and `bw sync` once
#   source Deployment/scripts/load-credentials.sh
#   (cd Deployment && pulumi stack output --stack production)
#   kubectl --kubeconfig "$PROMPTER_KUBECONFIG_PATH" -n prompter get pods
#   prompter_credentials_clear
#
# No file ever holds the values. Production `pulumi preview`, `up` and `refresh` run only in CI
# (Deploy - Production), never from a workstation; use this for `pulumi stack output`, `kubectl get` and
# `kubectl logs`.
#
# Bitwarden item names are placeholders. Override them per shell without editing this file:
#   PROMPTER_BW_PASSPHRASE_ITEM   item whose password is the Pulumi passphrase
#   PROMPTER_BW_STATE_ITEM        item holding the state backend key pair
#   PROMPTER_BW_STATE_ID_FIELD    field of that item for AWS_ACCESS_KEY_ID (default: username)
#   PROMPTER_BW_STATE_SECRET_FIELD field for AWS_SECRET_ACCESS_KEY (default: password)
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
  local passphrase_item="${PROMPTER_BW_PASSPHRASE_ITEM:-Cratis Pulumi passphrase (production)}"
  local state_item="${PROMPTER_BW_STATE_ITEM:-Cratis Pulumi state backend (UpCloud Object Storage)}"
  local id_field="${PROMPTER_BW_STATE_ID_FIELD:-username}"
  local secret_field="${PROMPTER_BW_STATE_SECRET_FIELD:-password}"
  local state_json

  command -v bw > /dev/null || { echo "bw (Bitwarden CLI) is required." >&2; return 1; }
  command -v jq > /dev/null || { echo "jq is required." >&2; return 1; }
  if [ -z "${BW_SESSION:-}" ]; then
    echo "Unlock first: export BW_SESSION=\"\$(bw unlock --raw)\" (and run bw sync)." >&2
    return 1
  fi

  PULUMI_CONFIG_PASSPHRASE="$(bw get password "${passphrase_item}")" || return 1
  state_json="$(bw get item "${state_item}")" || return 1
  AWS_ACCESS_KEY_ID="$(printf '%s' "${state_json}" | jq -r --arg f "${id_field}" \
    'if $f == "username" then .login.username elif $f == "password" then .login.password else (.fields // [] | map(select(.name == $f)) | .[0].value) end')" || return 1
  AWS_SECRET_ACCESS_KEY="$(printf '%s' "${state_json}" | jq -r --arg f "${secret_field}" \
    'if $f == "username" then .login.username elif $f == "password" then .login.password else (.fields // [] | map(select(.name == $f)) | .[0].value) end')" || return 1

  if [ -z "${PULUMI_CONFIG_PASSPHRASE}" ] || [ -z "${AWS_ACCESS_KEY_ID}" ] || [ "${AWS_ACCESS_KEY_ID}" = "null" ] \
    || [ -z "${AWS_SECRET_ACCESS_KEY}" ] || [ "${AWS_SECRET_ACCESS_KEY}" = "null" ]; then
    echo "A Bitwarden item or field came back empty; check the PROMPTER_BW_* item and field names." >&2
    return 1
  fi

  # Pulumi < 3.261.0 against an S3-compatible endpoint needs these (aws-sdk-go-v2 regression).
  AWS_REQUEST_CHECKSUM_CALCULATION=when_required
  AWS_RESPONSE_CHECKSUM_VALIDATION=when_required
  export PULUMI_CONFIG_PASSPHRASE AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY \
    AWS_REQUEST_CHECKSUM_CALCULATION AWS_RESPONSE_CHECKSUM_VALIDATION

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
