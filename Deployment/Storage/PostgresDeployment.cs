// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Apps.V1;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Apps.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Postgres with pgvector, as a single-replica StatefulSet with a persistent volume.
/// </summary>
/// <remarks>
/// In-cluster rather than managed, per D-11: it mirrors the MongoDB precedent on this cluster, and the
/// corpus is fully rebuildable from cratis.io, so the only data worth protecting is the (anonymous)
/// interaction log. Backups are therefore deliberately out of scope for the first cut — see the
/// operations table in <c>Planning/DEPLOYMENT.md</c>.
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
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [args.NamespaceResource] });

        // Headless: a single-replica StatefulSet needs a governing Service, and clients resolve the pod
        // through the same name. There is no load balancing to do with one replica.
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
                    ClusterIP = "None",
                    Selector = labels,
                    Ports = [new ServicePortArgs { Port = Port, TargetPort = Port, Name = "postgres" }],
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [args.NamespaceResource] });

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

        Resource = new StatefulSet(
            $"{Name}-{args.Environment}",
            new StatefulSetArgs
            {
                Metadata = new ObjectMetaArgs
                {
                    Name = Name,
                    Namespace = args.Namespace,
                    Labels = labels,
                },
                Spec = new StatefulSetSpecArgs
                {
                    ServiceName = Name,
                    Replicas = 1,
                    Selector = new LabelSelectorArgs { MatchLabels = labels },
                    Template = new PodTemplateSpecArgs
                    {
                        Metadata = new ObjectMetaArgs { Labels = labels },
                        Spec = new PodSpecArgs { Containers = [container] },
                    },
                    VolumeClaimTemplates =
                    [
                        new PersistentVolumeClaimArgs
                        {
                            Metadata = new ObjectMetaArgs { Name = "data", Namespace = args.Namespace },
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
                    ],
                },
            },
            new CustomResourceOptions { Provider = args.Provider, DependsOn = [secret, service] });

        ConnectionString = args.Password.Apply(password =>
            $"Host={Name};Port={Port};Database={DatabaseName};Username={UserName};Password={password}");
    }

    /// <summary>
    /// Gets the StatefulSet, so dependents can order themselves after it.
    /// </summary>
    public StatefulSet Resource { get; }

    /// <summary>
    /// Gets the Npgsql connection string the bot connects with.
    /// </summary>
    public Output<string> ConnectionString { get; }
}
