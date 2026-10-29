// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Apps.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using K8sDeployment = Pulumi.Kubernetes.Apps.V1.Deployment;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Postgres with pgvector, as a single-replica Deployment with a persistent volume claim.
/// </summary>
/// <remarks>
/// <para>
/// In-cluster rather than managed, per D-11: it mirrors the MongoDB precedent on this cluster, and the
/// corpus is fully rebuildable from cratis.io, so the only data worth protecting is the (anonymous)
/// interaction log. Backups are therefore deliberately out of scope for the first cut — see the
/// operations table in <c language="csharp">Planning/DEPLOYMENT.md</c>.
/// </para>
/// <para>
/// It is a Deployment rather than a StatefulSet because the deploy identity Cratis/Infrastructure's Core
/// stack grants every app has no <c language="csharp">statefulsets</c> permission. A single replica on a
/// ReadWriteOnce volume with the <c language="csharp">Recreate</c> strategy gives the same single-writer
/// guarantee: the old pod is gone before the new one mounts the volume.
/// </para>
/// </remarks>
public sealed class PostgresDeployment
{
    /// <summary>
    /// The name of the workload, its governing Service, and the in-cluster DNS name clients connect to.
    /// </summary>
    public const string Name = "postgres";

    /// <summary>
    /// The port Postgres listens on.
    /// </summary>
    public const int Port = 5432;

    const string DatabaseName = "prompter";
    const string UserName = "prompter";
    const string SecretName = "prompter-postgres";
    const string PasswordKey = "password";
    const string ClaimName = "postgres-data";

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresDeployment"/> class.
    /// </summary>
    /// <param name="args">The arguments describing the deployment.</param>
    public PostgresDeployment(PostgresDeploymentArgs args)
    {
        var labels = new InputMap<string>
        {
            ["app"] = Name,
            ["environment"] = args.Environment,
        };

        var secret = new Secret(
            $"{SecretName}-{args.Environment}",
            new SecretArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = SecretName,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                StringData = new InputMap<string> { [PasswordKey] = args.Password },
            },
            new CustomResourceOptions { Provider = args.Provider });

        // A plain ClusterIP Service: clients resolve the pod through the `postgres` name, and there is no
        // load balancing to do with one replica.
        var service = new Service(
            $"{Name}-service-{args.Environment}",
            new ServiceArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = Name,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                Spec = new ServiceSpecArgs
                {
                    Selector = labels,
                    Ports = [new ServicePortArgs { Port = Port, TargetPort = Port, Name = "postgres" }],
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [] });

        var container = new ContainerArgs
        {
            Name = Name,
            Image = args.Image,
            Ports = [new ContainerPortArgs { ContainerPortValue = Port, Name = "postgres" }],
            Env =
            [
                new EnvVarArgs { Name = "POSTGRES_DB", Value = DatabaseName },
                new EnvVarArgs { Name = "POSTGRES_USER", Value = UserName },
                new EnvVarArgs
                {
                    Name = "POSTGRES_PASSWORD",
                    ValueFrom = new EnvVarSourceArgs
                    {
                        SecretKeyRef = new SecretKeySelectorArgs { Name = SecretName, Key = PasswordKey },
                    },
                },

                // The volume is mounted at the data directory itself, which on a fresh UpCloud volume is
                // non-empty (lost+found) and would make initdb refuse. A subdirectory sidesteps that.
                new EnvVarArgs { Name = "PGDATA", Value = "/var/lib/postgresql/data/pgdata" },
            ],
            VolumeMounts = [new VolumeMountArgs { Name = "data", MountPath = "/var/lib/postgresql/data" }],
            ReadinessProbe = new ProbeArgs
            {
                Exec = new ExecActionArgs { Command = ["pg_isready", "-U", UserName, "-d", DatabaseName] },
                InitialDelaySeconds = 5,
                PeriodSeconds = 10,
            },
            LivenessProbe = new ProbeArgs
            {
                Exec = new ExecActionArgs { Command = ["pg_isready", "-U", UserName, "-d", DatabaseName] },
                InitialDelaySeconds = 30,
                PeriodSeconds = 30,
                FailureThreshold = 6,
            },
            Resources = new ResourceRequirementsArgs
            {
                Requests = { ["cpu"] = "100m", ["memory"] = "256Mi" },
                Limits = { ["cpu"] = "1", ["memory"] = "1Gi" },
            },
        };

        // Created ahead of the Deployment. A volume that binds only when its first consumer is scheduled
        // would otherwise leave Pulumi waiting on a claim that nothing is using yet.
        var claim = new PersistentVolumeClaim(
            $"{ClaimName}-{args.Environment}",
            new PersistentVolumeClaimArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = ClaimName,
                    Namespace = args.Namespace,
                    Labels = labels,
                    Annotations = { ["pulumi.com/skipAwait"] = "true" },
                },
                Spec = new PersistentVolumeClaimSpecArgs
                {
                    AccessModes = ["ReadWriteOnce"],
                    StorageClassName = args.StorageClassName,
                    Resources = new VolumeResourceRequirementsArgs
                    {
                        Requests = { ["storage"] = $"{args.StorageSizeGb}Gi" },
                    },
                },
            },
            new CustomResourceOptions { Provider = args.Provider });

        Resource = new K8sDeployment(
            $"{Name}-{args.Environment}",
            new DeploymentArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = Name,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                Spec = new DeploymentSpecArgs
                {
                    Replicas = 1,
                    Strategy = new DeploymentStrategyArgs { Type = "Recreate" },
                    Selector = new LabelSelectorArgs { MatchLabels = labels },
                    Template = new PodTemplateSpecArgs
                    {
                        Metadata = new ObjectMetaArgs { Labels = labels },
                        Spec = new PodSpecArgs
                        {
                            Containers = [container],
                            Volumes =
                            [
                                new VolumeArgs
                                {
                                    Name = "data",
                                    PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceArgs { ClaimName = ClaimName },
                                },
                            ],
                        },
                    },
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [secret, service, claim] });

        ConnectionString = args.Password.Apply(password =>
            $"Host={Name};Port={Port};Database={DatabaseName};Username={UserName};Password={password}");
    }

    /// <summary>
    /// Gets the Deployment, so dependents can order themselves after it.
    /// </summary>
    public K8sDeployment Resource { get; }

    /// <summary>
    /// Gets the Npgsql connection string the bot connects with.
    /// </summary>
    public Output<string> ConnectionString { get; }
}
