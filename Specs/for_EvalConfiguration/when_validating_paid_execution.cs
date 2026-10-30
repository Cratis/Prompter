// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval;
using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Specs.for_EvalConfiguration;

public class when_validating_paid_execution : Specification
{
    EvalConfiguration _configuration = null!;

    void Establish() => _configuration = EvalConfiguration.From(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Cratis:Prompter:ConnectionString"] = "production-poison",
        ["Cratis:Prompter:Anthropic:ApiKey"] = "production-poison",
        ["ConnectionString"] = "cli-poison"
    }).Build());

    [Fact] void should_not_bind_production_values() => _configuration.ConnectionString.ShouldEqual(string.Empty);
    [Fact] void should_not_authorize_paid_execution() => _configuration.PaidBlockReason.ShouldEqual("Missing eval-only settings.");
}
