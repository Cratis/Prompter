// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_the_unknown_custom_id_is_in_a_dm : given.a_button_dispatcher
{
    async Task Because() => await DispatchClick("retired-button:0", guild: null);

    [Fact] void should_dispatch_the_click() => Dispatches.ShouldEqual(1);
    [Fact] void should_send_no_rest_requests() => Paths.ShouldBeEmpty();
    [Fact] void should_send_no_payloads() => Payloads.ShouldBeEmpty();
    [Fact] void should_skip_the_result_handler() => Results.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_never_resolve_module_dependencies() => DependencyResolutions.ShouldEqual(0);
    [Fact] void should_do_no_paid_work() => NoPaidWork();
}
