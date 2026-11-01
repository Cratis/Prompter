// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Discord;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services;
using NetCord.Services.ComponentInteractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_a_known_module_cannot_be_activated : given.a_button_dispatcher
{
    JsonElement _reply;
    string _expected = null!;

    async Task Because()
    {
        await DispatchClick(IssueButton.Prefix);
        var failure = (IFailResult)Results.ReceivedCalls().Single().GetArguments()[0]!;
        var expected = await ComponentInteractionResultHandler<ButtonInteractionContext>.Default.GetFailMessageAsync(failure, ButtonContext, null!);
        _expected = expected.Content!;
        _reply = JsonSerializer.Deserialize<JsonElement>(Payloads.Single()).GetProperty("data");
    }

    [Fact] void should_acknowledge_once() => Paths.Count.ShouldEqual(1);
    [Fact] void should_remain_an_execution_failure() => (Results.ReceivedCalls().Single().GetArguments()[0] is ExecutionExceptionResult).ShouldBeTrue();
    [Fact] void should_preserve_the_default_message() => _reply.GetProperty("content").GetString().ShouldEqual(_expected);
    [Fact] void should_not_use_the_expired_reply() => _reply.GetProperty("content").GetString().ShouldNotEqual(ButtonResults.Expired);
    [Fact] void should_not_change_the_default_visibility() => _reply.TryGetProperty("flags", out _).ShouldBeFalse();
    [Fact] void should_never_resolve_module_dependencies() => DependencyResolutions.ShouldEqual(0);
}
