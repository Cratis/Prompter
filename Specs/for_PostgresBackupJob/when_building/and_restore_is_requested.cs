// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Prompter.Deployment.Storage;

namespace Cratis.Prompter.Specs.for_PostgresBackupJob.when_building;

public class and_restore_is_requested : Specification
{
    JsonNode _container = null!;
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
        var job = JsonNode.Parse(PostgresBackupJob.Build(args, true))!;
        _pod = job["spec"]!["template"]!["spec"]!;
        _container = _pod["containers"]![0]!;
    }

    [Fact] void should_not_receive_a_production_connection() => _container["env"]!.AsArray().Any(value => value!["name"]!.GetValue<string>().StartsWith("PG", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_use_a_readonly_store_identity() => _container["envFrom"]![0]!["secretRef"]!["name"]!.GetValue<string>().ShouldEqual("prompter-backup-store-reader");
    [Fact] void should_only_mount_the_restore_identity() => _pod["volumes"]![1]!["secret"]!["secretName"]!.GetValue<string>().ShouldEqual("prompter-backup-identity");
    [Fact] void should_not_select_an_archive_implicitly() => _container["env"]!.AsArray().Any(value => value!["name"]!.GetValue<string>() == "BACKUP_ID").ShouldBeFalse();
}
