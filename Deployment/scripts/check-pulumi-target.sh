#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
# Admits a recovery deploy target only when it is empty or exactly one Pulumi resource URN.
# The guarded Pulumi action splits its arguments on whitespace, so anything else could add flags.
set -euo pipefail

target="${1-}"
if [ -z "$target" ]; then
    exit 0
fi
if [[ "$target" =~ ^urn:pulumi:[^[:space:]]+$ ]]; then
    exit 0
fi
echo "::error::target must be a single Pulumi resource URN (urn:pulumi:...) without whitespace" >&2
exit 1
