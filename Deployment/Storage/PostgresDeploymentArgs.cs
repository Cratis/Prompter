// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Arguments for <see cref="PostgresDeployment"/>.
/// </summary>
public sealed class PostgresDeploymentArgs
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
    /// Gets the resource the namespace is created by, so the database is never created before it.
    /// </summary>
    public required Resource NamespaceResource { get; init; }

    /// <summary>
    /// Gets the password for the <c>prompter</c> database role.
    /// </summary>
    public required Output<string> Password { get; init; }

    /// <summary>
    /// Gets the name of the storage class volumes are provisioned from. This references a cluster-scoped
    /// StorageClass owned by Studio's stack (<c>upcloud-maxiops</c>) — it is never created here.
    /// </summary>
    public required string StorageClassName { get; init; }

    /// <summary>
    /// Gets the image to run. The corpus needs the <c>vector</c> extension, so this is a pgvector build
    /// rather than stock Postgres — the same image the local <c>docker-compose.yml</c> and the eval
    /// workflow use, so all three environments agree.
    /// </summary>
    public string Image { get; init; } = "pgvector/pgvector:pg17";

    /// <summary>
    /// Gets the size of the data volume in gigabytes. The corpus is ~20k chunks with 1024-dimension
    /// embeddings, so this is generous; it can only ever grow (UpCloud volumes expand, never shrink).
    /// </summary>
    public int StorageSizeGb { get; init; } = 10;
}
