// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_the_channel_is_denied : given.a_button_dispatcher
{
    void Establish() => Options.Protection.DeniedChannels = [70];
    async Task Because() => await Dispatch();

    [Fact] void should_exercise_guidance_and_feedback() => Dispatches.ShouldEqual(3);
    [Fact] void should_send_no_rest_requests() => Paths.ShouldBeEmpty();
    [Fact] void should_send_no_payloads() => Payloads.ShouldBeEmpty();
    [Fact] void should_never_resolve_module_dependencies() => DependencyResolutions.ShouldEqual(0);
    [Fact] void should_skip_the_sdk_result_handler() => Results.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_never_write_feedback() => Interactions.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_do_no_paid_work() => NoPaidWork();
}
