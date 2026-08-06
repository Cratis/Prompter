// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>
/// Arguments for <see cref="PrompterIngress"/>.
/// </summary>
public sealed class PrompterIngressArgs
{
    /// <summary>
    /// Gets the Kubernetes provider to deploy through.
    /// </summary>
    public required KubernetesProvider Provider { get; init; }

    /// <summary>
    /// Gets the namespace to deploy into.
    /// </summary>
    public required string Namespace { get; init; }

    /// <summary>
    /// Gets the environment label.
    /// </summary>
    public required string Environment { get; init; }

    /// <summary>
    /// Gets the public host name routed to Prompter. A DNS record for it must point at the cluster's
    /// existing ingress load balancer.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Gets the name of the Service to route to.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// Gets the port to route to.
    /// </summary>
    public required int ServicePort { get; init; }

    /// <summary>
    /// Gets resources the ingress must be created after.
    /// </summary>
    public InputList<Resource> DependsOn { get; init; } = [];
}
