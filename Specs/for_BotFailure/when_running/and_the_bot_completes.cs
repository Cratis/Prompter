// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Hosting;

namespace Cratis.Prompter.Specs.for_BotFailure.when_running;

public class and_the_bot_completes : Specification
{
    readonly StringWriter _output = new();
    int _exit;
    async Task Because() => _exit = await BotFailure.Run(() => Task.CompletedTask, _output);

    [Fact] void should_exit_zero() => _exit.ShouldEqual(0);
    [Fact] void should_write_nothing() => _output.ToString().ShouldEqual(string.Empty);
}
