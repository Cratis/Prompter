// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Builds inert Job templates; scheduling uses the existing Jobs-only deploy identity.
/// </summary>
public static class PostgresBackupJob
{
    static readonly string[] _droppedCapabilities = ["ALL"];

    /// <summary>
    /// Creates a bounded backup or local-only restore verification Job without deploying it.
    /// </summary>
    /// <param name="args">The explicitly approved backup configuration.</param>
    /// <param name="restore">Whether to verify an existing encrypted archive.</param>
    /// <returns>The Kubernetes Job JSON.</returns>
    /// <exception cref="InvalidPostgresBackupConfiguration">The public configuration is not sufficiently scoped.</exception>
    public static string Build(PostgresBackupsArgs args, bool restore)
    {
        args.Validate();
        var name = restore ? "prompter-postgres-restore" : "prompter-postgres-backup";
        var env = new List<object>
        {
            new { name = "BACKUPS_ENABLED", value = "true" },
            new { name = "BACKUP_ENDPOINT", value = args.Endpoint },
            new { name = "BACKUP_BUCKET", value = args.Bucket },
            new { name = "TMPDIR", value = "/work" },
            new { name = "HOME", value = "/work" },
        };
        env.AddRange(restore
            ? [new { name = "RESTORE_APPROVED", value = "isolated-local-only" }, new { name = "AGE_IDENTITY", value = "/identity/key.txt" }]
            : new[]
            {
                new { name = "AGE_RECIPIENT", value = args.Recipient },
                new { name = "PGHOST", value = "postgres" },
                new { name = "PGDATABASE", value = "prompter" },
                new { name = "PGUSER", value = "prompter_backup" },
                new { name = "PGPASSFILE", value = "/identity/pgpass" },
            });
        var script = restore ? "restore-postgres-test.sh" : "backup-postgres.sh";
        var command = $"exec timeout --signal=TERM --kill-after=30s 3300 bash /scripts/{script}";
        if (!restore)
        {
            command = $"export BACKUP_ID=$(date -u +%Y%m%dT%H%M%SZ); {command}";
        }

        return JsonSerializer.Serialize(new
        {
            apiVersion = "batch/v1",
            kind = "Job",
            metadata = new { name, @namespace = args.Namespace },
            spec = new
            {
                backoffLimit = 0,
                activeDeadlineSeconds = 3600,
                ttlSecondsAfterFinished = 3600,
                template = new
                {
                    metadata = new { labels = new { app = name } },
                    spec = new
                    {
                        restartPolicy = "Never",
                        automountServiceAccountToken = false,
                        securityContext = new { runAsNonRoot = true },
                        containers = new[]
                        {
                            new
                            {
                                name = "postgres-backup",
                                image = args.Image,
                                command = new[] { "bash", "-c", command },
                                env,
                                envFrom = new[] { new { secretRef = new { name = restore ? "prompter-backup-store-reader" : "prompter-backup-store-writer" } } },
                                securityContext = new { allowPrivilegeEscalation = false, readOnlyRootFilesystem = true, capabilities = new { drop = _droppedCapabilities }, seccompProfile = new { type = "RuntimeDefault" } },
                                resources = new { requests = new { cpu = "100m", memory = "512Mi" }, limits = new { cpu = "2", memory = "4Gi" } },
                                volumeMounts = new[]
                                {
                                    new { name = "scripts", mountPath = "/scripts", readOnly = true },
                                    new { name = "identity", mountPath = "/identity", readOnly = true },
                                    new { name = "work", mountPath = "/work", readOnly = false },
                                },
                            },
                        },
                        volumes = new object[]
                        {
                            new { name = "scripts", configMap = new { name = "prompter-postgres-backups", defaultMode = 292 } },
                            new { name = "identity", secret = new { secretName = restore ? "prompter-backup-identity" : "prompter-postgres-backup-reader", defaultMode = 292 } },
                            new { name = "work", emptyDir = new { medium = "Memory", sizeLimit = "2Gi" } },
                        },
                    },
                },
            },
        });
    }
}
