// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Hosting;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_BotFailure.when_running;

public class and_the_bot_throws : Specification
{
    readonly StringWriter _output = new();
    int _exit;
    async Task Because()
    {
        var key = string.Concat("api", "_key");
        const string Value = "placeholder-value";
        var message = key + "=" + Value;
        _exit = await BotFailure.Run(() => throw new InvalidOperationException(message), _output);
    }

    [Fact] void should_exit_one() => _exit.ShouldEqual(1);
    [Fact] void should_report_one_redacted_line() => _output.ToString().ShouldEqual($"Bot failed (InvalidOperationException): api_key={SecretRedaction.Marker}{Environment.NewLine}");
    [Fact] void should_not_report_the_value() => _output.ToString().Contains("placeholder-value", StringComparison.Ordinal).ShouldBeFalse();
}
