// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using UpCloud.Pulumi.UpCloud;
using K8sProviderArgs = Pulumi.Kubernetes.ProviderArgs;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Cluster;

/// <summary>
/// The UpCloud Kubernetes cluster Prompter deploys into — looked up, never created.
/// </summary>
/// <remarks>
/// The cluster belongs to Studio's Pulumi stack (decision D-11/D-15). This stack only resolves its
/// kubeconfig so it can create namespaced resources inside it; it deliberately declares nothing
/// cluster-scoped, which is what keeps the two stacks from fighting over shared state.
/// </remarks>
/// <param name="clusterId">The UpCloud UKS cluster id to deploy into.</param>
public sealed class ExistingCluster(string clusterId)
{
    /// <summary>
    /// Gets the kubeconfig of the existing cluster.
    /// </summary>
    public Output<string> Kubeconfig { get; } = GetKubernetesCluster
        .Invoke(new GetKubernetesClusterInvokeArgs { Id = clusterId })
        .Apply(cluster => cluster.Kubeconfig);

    /// <summary>
    /// Creates the Kubernetes provider every resource in this stack is created through.
    /// </summary>
    /// <param name="environment">The environment name, used to name the provider.</param>
    /// <returns>The <see cref="KubernetesProvider"/> for the cluster.</returns>
    public KubernetesProvider CreateProvider(string environment) =>
        new($"k8s-{environment}", new K8sProviderArgs { KubeConfig = Kubeconfig });
}
