// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>Builds a collector rule only for a complete explicitly enabled destination.</summary>
public static class PrivateMetricsEgress
{
    /// <summary>Projects an AND-matched namespace and pod peer with one verified TCP backend port.</summary>
    /// <param name="args">The same collector identity used by the runtime environment projector.</param>
    /// <returns>A single narrow rule, or no grant for disabled or invalid settings.</returns>
    public static NetworkPolicyEgressRuleArgs? Create(PrivateMetricsArgs args) => !args.Enabled || !args.IsValid ? null : new()
    {
        To =
        [
            new NetworkPolicyPeerArgs
            {
                NamespaceSelector = new LabelSelectorArgs { MatchLabels = { ["kubernetes.io/metadata.name"] = args.Namespace } },
                PodSelector = new LabelSelectorArgs { MatchLabels = { ["app"] = args.App, ["environment"] = args.Environment } }
            }
        ],
        Ports = [new NetworkPolicyPortArgs { Protocol = "TCP", Port = args.BackendPort }]
    };
}
