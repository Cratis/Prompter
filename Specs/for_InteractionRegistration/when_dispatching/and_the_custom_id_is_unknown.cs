// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Discord;
using NetCord;
using NetCord.Services;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_the_custom_id_is_unknown : given.a_button_dispatcher
{
    const string Unknown = "retired-button:do-not-echo";
    JsonElement _reply;

    async Task Because()
    {
        await DispatchClick(Unknown);
        _reply = JsonSerializer.Deserialize<JsonElement>(Payloads.Single());
    }

    [Fact] void should_acknowledge_once() => Paths.Count.ShouldEqual(1);
    [Fact] void should_send_a_message_callback() => _reply.GetProperty("type").GetInt32().ShouldEqual(4);
    [Fact] void should_send_the_friendly_reply() => _reply.GetProperty("data").GetProperty("content").GetString().ShouldEqual(ButtonResults.Expired);
    [Fact] void should_keep_the_reply_ephemeral() => _reply.GetProperty("data").GetProperty("flags").GetInt32().ShouldEqual((int)MessageFlags.Ephemeral);
    [Fact] void should_not_echo_the_custom_id() => Payloads.Single().Contains(Unknown, StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_take_the_not_found_result_path() => (Results.ReceivedCalls().Single().GetArguments()[0] is NotFoundResult).ShouldBeTrue();
    [Fact] void should_never_resolve_module_dependencies() => DependencyResolutions.ShouldEqual(0);
    [Fact] void should_do_no_paid_work() => NoPaidWork();
}
