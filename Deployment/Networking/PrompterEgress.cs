// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Networking.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>
/// Outbound HTTPS for the Prompter bot.
/// </summary>
/// <remarks>
/// Core's baseline <c language="csharp">default-deny</c> policy permits only DNS, same-namespace and
/// Infrastructure traffic. The bot dials out to the Discord gateway and REST API, Anthropic, Voyage, the
/// documentation site and GitHub, so this grants outbound TCP 443 to public addresses only, without
/// opening the private ranges. Postgres needs nothing extra: same-namespace egress is already allowed.
/// </remarks>
public sealed class PrompterEgress
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PrompterEgress"/> class.
    /// </summary>
    /// <param name="args">The arguments describing the policy.</param>
    public PrompterEgress(PrompterEgressArgs args)
    {
        _ = new NetworkPolicy(
            $"prompter-external-https-{args.Environment}",
            new NetworkPolicyArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = "prompter-external-https",
                    Namespace = args.Namespace,
                    Labels = { ["environment"] = args.Environment, ["app"] = "prompter" },
                },
                Spec = new NetworkPolicySpecArgs
                {
                    PodSelector = new LabelSelectorArgs { MatchLabels = { ["app"] = "prompter" } },
                    PolicyTypes = ["Egress"],
                    Egress =
                    [
                        new NetworkPolicyEgressRuleArgs
                        {
                            To =
                            [
                                new NetworkPolicyPeerArgs
                                {
                                    IpBlock = new IPBlockArgs
                                    {
                                        Cidr = "0.0.0.0/0",
                                        Except = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"],
                                    },
                                },
                            ],
                            Ports = [new NetworkPolicyPortArgs { Protocol = "TCP", Port = 443 }],
                        },
                    ],
                },
            },
            new CustomResourceOptions { Provider = args.Provider });
    }
}
