// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_PaidWork.when_reserving;

public class and_the_ledger_refuses : Specification
{
    PaidWork _work = null!;
    int _calls;
    Exception? _error;

    void Establish()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.Protection.Anthropic.Model = options.Value.Anthropic.Model;
        options.Value.Protection.Anthropic.InputUsdPerMillion = 1;
        options.Value.Protection.Anthropic.OutputUsdPerMillion = 1;
        options.Value.Protection.Anthropic.MaxInputTokens = 32000;
        options.Value.Protection.Voyage.Model = options.Value.Voyage.Model;
        options.Value.Protection.Voyage.InputUsdPerMillion = 1;
        options.Value.Protection.Voyage.MaxInputTokens = 16000;
        var ledger = Substitute.For<ISpendLedger>();
        ledger.Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);
        _work = new(ledger, options, TimeProvider.System);
    }

    async Task Because() => _error = await Catch.Exception(async () =>
    {
        await using var lease = await _work.BeginInteractive();
        _calls++;
    });

    [Fact] void should_make_zero_downstream_calls() => _calls.ShouldEqual(0);
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WorkRefused>();
}
