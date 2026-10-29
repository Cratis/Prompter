#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Builds the namespace-scoped kubeconfig the Prompter deploy workflow runs as.
#
# Cratis/Infrastructure's Core stack (AppAccess) creates the `prompter-deploy` ServiceAccount and its
# `prompter-deploy-token` Secret in the `prompter` namespace. The token controller fills that Secret
# asynchronously, so Core only declares it; this script reads the populated values afterwards and writes
# a kubeconfig. Run it with Infrastructure's admin access, once, then hand the result to GitHub as the
# KUBECONFIG secret.
#
# Requires: kubectl, and INFRASTRUCTURE_KUBECONFIG (default ~/.studio-kube/config) as the admin kubeconfig Infrastructure's
# scripts/setup-local-env.sh writes. Nothing secret is printed, and nothing is written inside this repository.
#
#   INFRASTRUCTURE_KUBECONFIG=~/.studio-kube/config Deployment/scripts/generate-prompter-kubeconfig.sh
set -euo pipefail
# Never trace: tracing (for example from SHELLOPTS or bash -x) would print the token.
{ set +x; } 2> /dev/null

namespace="${PROMPTER_NAMESPACE:-prompter}"
secret_name="${PROMPTER_DEPLOY_TOKEN_SECRET:-prompter-deploy-token}"
user_name="${PROMPTER_DEPLOY_USER:-prompter-deploy}"
output_dir="${PROMPTER_KUBE_DIR:-$HOME/.prompter-kube}"
output_file="${output_dir}/config"

if ! command -v kubectl > /dev/null; then
  echo "kubectl is required." >&2
  exit 1
fi
INFRASTRUCTURE_KUBECONFIG="${INFRASTRUCTURE_KUBECONFIG:-$HOME/.studio-kube/config}"
if [ -z "${INFRASTRUCTURE_KUBECONFIG:-}" ] || [ ! -r "${INFRASTRUCTURE_KUBECONFIG}" ]; then
  echo "Set INFRASTRUCTURE_KUBECONFIG to the admin kubeconfig written by Infrastructure's scripts/setup-local-env.sh." >&2
  exit 1
fi

# The admin kubeconfig is only used to find the API server and to read the token Secret.
admin() { kubectl --kubeconfig "${INFRASTRUCTURE_KUBECONFIG}" "$@"; }

server="$(admin config view --minify --raw -o jsonpath='{.clusters[0].cluster.server}')"
token_b64="$(admin -n "${namespace}" get secret "${secret_name}" -o jsonpath='{.data.token}')"
ca_b64="$(admin -n "${namespace}" get secret "${secret_name}" -o jsonpath='{.data.ca\.crt}')"

if [ -z "${server}" ] || [ -z "${token_b64}" ] || [ -z "${ca_b64}" ]; then
  echo "The ${secret_name} Secret in ${namespace} is not populated yet, or the admin kubeconfig has no server." >&2
  echo "If Core has just been deployed, wait a moment and run this again." >&2
  exit 1
fi

token="$(printf '%s' "${token_b64}" | base64 --decode)"

umask 077
mkdir -p "${output_dir}"
chmod 700 "${output_dir}"
cat > "${output_file}" << KUBECONFIG
apiVersion: v1
kind: Config
clusters:
  - name: cratis-shared
    cluster:
      server: ${server}
      certificate-authority-data: ${ca_b64}
users:
  - name: ${user_name}
    user:
      token: ${token}
contexts:
  - name: ${user_name}@cratis-shared
    context:
      cluster: cratis-shared
      user: ${user_name}
      namespace: ${namespace}
current-context: ${user_name}@cratis-shared
KUBECONFIG
chmod 600 "${output_file}"
unset token token_b64 ca_b64

# The identity must be able to deploy into its namespace and must not reach beyond it.
if [ "$(kubectl --kubeconfig "${output_file}" auth can-i create deployments -n "${namespace}")" != "yes" ]; then
  echo "The generated kubeconfig cannot create deployments in ${namespace}." >&2
  exit 1
fi
if [ "$(kubectl --kubeconfig "${output_file}" auth can-i get nodes || true)" != "no" ]; then
  echo "The generated kubeconfig can read nodes; it should be namespace-scoped." >&2
  exit 1
fi

echo "Wrote ${output_file} (mode 600). The identity can deploy into ${namespace} and cannot read nodes."
echo "Next, hand it to GitHub (not run for you):"
echo "  gh secret set KUBECONFIG --repo Cratis/Prompter < ${output_file}"
