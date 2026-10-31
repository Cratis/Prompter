// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Networking.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Pulumi.Kubernetes.Types.Inputs.Networking.V1;
using KubernetesProvider = Pulumi.Kubernetes.Provider;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Opt-in inert templates and scoped store egress, without CronJob RBAC or credentials in Pulumi state.
/// </summary>
public sealed class PostgresBackups
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresBackups"/> class.
    /// </summary>
    /// <param name="args">The approved public configuration.</param>
    /// <param name="provider">The existing namespace-scoped provider.</param>
    public PostgresBackups(PostgresBackupsArgs args, KubernetesProvider provider)
    {
        var data = new InputMap<string>
        {
            ["backup-job.json"] = PostgresBackupJob.Build(args, false),
            ["restore-job.json"] = PostgresBackupJob.Build(args, true),
        };
        foreach (var name in new[] { "backup-postgres.sh", "restore-postgres-test.sh", "postgres-backup-common.sh", "postgres-backup-metadata.sql" })
        {
            using var stream = typeof(PostgresBackups).Assembly.GetManifestResourceStream($"PostgresBackups.{name}")!;
            using var reader = new StreamReader(stream);
            data[name] = reader.ReadToEnd();
        }

        _ = new ConfigMap(
            $"postgres-backups-{args.Environment}",
            new ConfigMapArgs
            {
                Metadata = new ObjectMetaArgs { Name = "prompter-postgres-backups", Namespace = args.Namespace },
                Data = data,
            },
            new CustomResourceOptions { Provider = provider });

        _ = new NetworkPolicy(
            $"postgres-backup-store-{args.Environment}",
            new NetworkPolicyArgs
            {
                Metadata = new ObjectMetaArgs { Name = "prompter-postgres-backup-store", Namespace = args.Namespace },
                Spec = new NetworkPolicySpecArgs
                {
                    PodSelector = new LabelSelectorArgs
                    {
                        MatchExpressions = [new LabelSelectorRequirementArgs { Key = "app", Operator = "In", Values = ["prompter-postgres-backup", "prompter-postgres-restore"] }],
                    },
                    PolicyTypes = ["Egress"],
                    Egress =
                [
                    new NetworkPolicyEgressRuleArgs
                    {
                        To = [new NetworkPolicyPeerArgs { IpBlock = new IPBlockArgs { Cidr = args.StoreCidr } }],
                        Ports = [new NetworkPolicyPortArgs { Protocol = "TCP", Port = 443 }],
                    },
                ],
                },
            },
            new CustomResourceOptions { Provider = provider });
    }
}
