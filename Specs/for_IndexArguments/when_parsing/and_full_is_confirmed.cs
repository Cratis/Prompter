// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexArguments.when_parsing;

public class and_full_is_confirmed : Specification
{
    readonly string[] _args = ["index", "--full", "--confirm-full-reindex=FULL REINDEX", "--Cratis:Prompter:Voyage:BatchSize=1"];
    IndexRequest _request = null!;
    string[] _configuration = null!;

    void Because()
    {
        _request = IndexArguments.Parse(_args);
        _configuration = IndexArguments.ConfigurationArguments(_args);
    }

    [Fact] void should_request_full() => _request.Full.ShouldBeTrue();
    [Fact] void should_preserve_the_literal_consent() => _request.Confirmation.ShouldEqual("FULL REINDEX");
    [Fact] void should_preserve_configuration_overrides() => _configuration.ShouldEqual(["index", "--Cratis:Prompter:Voyage:BatchSize=1"]);
}
