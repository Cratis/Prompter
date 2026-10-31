// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_reserving;

public class metadata_is_not_authorized : given.durable_accounting
{
    [Theory]
    [InlineData("provider")]
    [InlineData("model")]
    [InlineData("account")]
    [InlineData("task")]
    [InlineData("effort")]
    [InlineData("attempt")]
    [InlineData("preferred")]
    [InlineData("price")]
    [InlineData("ceiling")]
    public async Task should_deny_before_persistence(string invalid)
    {
        ConfigureInvalidPolicy(invalid);
        var dispatch = InvalidDispatch(invalid);
        using var scope = ProviderAttemptScope.Enter(dispatch);
        (await Accounting.Reserve(dispatch, TestContext.Current.CancellationToken)).ShouldBeFalse();
        await Store.DidNotReceive().Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    void ConfigureInvalidPolicy(string invalid)
    {
        if (invalid == "preferred")
        {
            Options.Anthropic.Answer.PreferredAccount = "unknown";
        }

        if (invalid == "price")
        {
            Options.Protection.AnthropicAnswer.InputUsdPerMillion = 0;
        }

        if (invalid == "ceiling")
        {
            Options.Protection.DailyUsd = 6;
        }
    }

    ProviderDispatch InvalidDispatch(string invalid)
    {
        var valid = Dispatch();
        var mutations = new Dictionary<string, ProviderDispatch>(StringComparer.Ordinal)
        {
            ["provider"] = valid with { Provider = "unknown" },
            ["model"] = valid with { Model = "unknown" },
            ["account"] = valid with { Account = "unknown" },
            ["task"] = valid with { Task = "unknown" },
            ["effort"] = valid with { Effort = "unknown" },
            ["attempt"] = valid with { Attempt = 3 }
        };
        return mutations.GetValueOrDefault(invalid, valid);
    }
}
