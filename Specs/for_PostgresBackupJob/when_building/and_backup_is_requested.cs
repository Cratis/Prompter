// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prompter.Deployment.Storage;

namespace Cratis.Prompter.Specs.for_PostgresBackupJob.when_building;

public class and_backup_is_requested : Specification
{
    JsonNode _job = null!;
    JsonNode _pod = null!;

    void Because()
    {
        var args = new PostgresBackupsArgs
        {
            Namespace = "prompter",
            Environment = "test",
            Image = $"tooling@sha256:{new string('a', 64)}",
            Endpoint = "https://storage.invalid",
            StoreCidr = "203.0.113.9/32",
            Bucket = "offline-fixture",
            Recipient = "age1fixture",
        };
        _job = JsonNode.Parse(PostgresBackupJob.Build(args, false))!;
        _pod = _job["spec"]!["template"]!["spec"]!;
    }

    [Fact] void should_have_a_fixed_name() => _job["metadata"]!["name"]!.GetValue<string>().ShouldEqual("prompter-postgres-backup");
    [Fact] void should_not_retry_a_partial_backup() => _job["spec"]!["backoffLimit"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_have_a_hard_deadline() => _job["spec"]!["activeDeadlineSeconds"]!.GetValue<int>().ShouldEqual(3600);
    [Fact] void should_expire_terminal_jobs() => _job["spec"]!["ttlSecondsAfterFinished"]!.GetValue<int>().ShouldEqual(3600);
    [Fact] void should_not_mount_a_cluster_token() => _pod["automountServiceAccountToken"]!.GetValue<bool>().ShouldBeFalse();
    [Fact] void should_run_as_nonroot() => _pod["securityContext"]!["runAsNonRoot"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_have_a_readonly_root() => _pod["containers"]![0]!["securityContext"]!["readOnlyRootFilesystem"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_bound_plaintext_memory() => _pod["volumes"]![2]!["emptyDir"]!["sizeLimit"]!.GetValue<string>().ShouldEqual("2Gi");
    [Fact] void should_not_mount_a_persistent_volume() => _pod["volumes"]!.AsArray().Any(volume => volume!["persistentVolumeClaim"] is not null).ShouldBeFalse();
    [Fact] void should_use_the_separate_writer() => _pod["containers"]![0]!["envFrom"]![0]!["secretRef"]!["name"]!.GetValue<string>().ShouldEqual("prompter-backup-store-writer");
}
