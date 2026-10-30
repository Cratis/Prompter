// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Storage;

namespace Cratis.Prompter.Specs.for_PostgresBackupJob.when_building;

public class and_configuration_is_unsafe : Specification
{
    readonly List<Exception> _failures = [];

    void Because()
    {
        var valid = new PostgresBackupsArgs
        {
            Namespace = "prompter",
            Environment = "test",
            Image = $"tooling@sha256:{new string('a', 64)}",
            Endpoint = "https://storage.invalid",
            StoreCidr = "203.0.113.9/32",
            Bucket = "offline-fixture",
            Recipient = "age1fixture",
        };
        foreach (var args in new[]
        {
            valid with { Image = "tooling:latest" },
            valid with { Endpoint = "http://storage.invalid" },
            valid with { StoreCidr = "0.0.0.0/0" },
            valid with { StoreCidr = "127.0.0.1/32" },
            valid with { StoreCidr = "10.0.0.1/32" },
            valid with { StoreCidr = "100.64.0.1/32" },
            valid with { Recipient = "AGE-SECRET-KEY-not-a-real-key" },
        })
        {
            _failures.Add(Record.Exception(() => PostgresBackupJob.Build(args, false))!);
        }
    }

    [Fact] void should_reject_every_unsafe_configuration() => _failures.TrueForAll(failure => failure is InvalidPostgresBackupConfiguration).ShouldBeTrue();
}
