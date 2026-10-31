// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Networking;

/// <summary>
/// Arguments for <see cref="PrompterEgress"/>.
/// </summary>
public sealed class PrompterEgressArgs
{
    /// <summary>Gets the explicit private collector identity; absent settings grant nothing.</summary>
    public PrivateMetricsArgs Metrics { get; init; } = new();

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
}
