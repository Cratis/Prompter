// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>Builds a bot-only policy for the same explicitly approved runtime destination.</summary>
public static class PrivateRoutingEgress
{
    /// <summary>Projects one AND-matched peer and verified TCP backend port, or no policy.</summary>
    /// <param name="args">The private routing identity.</param>
    /// <param name="environment">The source bot environment.</param>
    /// <returns>A narrow bot-only policy, or no grant for absent or invalid settings.</returns>
    public static NetworkPolicySpecArgs? Create(PrivateRoutingArgs args, string environment) => !args.IsConfigured || !args.IsValid ? null : new()
    {
        PodSelector = new LabelSelectorArgs { MatchLabels = { ["app"] = "prompter", ["environment"] = environment } },
        PolicyTypes = ["Egress"],
        Egress =
        [
            new NetworkPolicyEgressRuleArgs
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
            }
        ]
    };
}
