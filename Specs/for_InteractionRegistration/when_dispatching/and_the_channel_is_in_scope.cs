// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;
using NetCord.Services;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_InteractionRegistration.when_dispatching;

public class and_the_channel_is_in_scope : given.a_button_dispatcher
{
    async Task Because() => await Dispatch();

    [Fact] void should_dispatch_both_guidance_methods_and_feedback() => DependencyResolutions.ShouldEqual(3);
    [Fact] void should_acknowledge_each_click() => Paths.Count.ShouldEqual(3);
    [Fact] void should_preserve_the_suggestion_notice() => Payloads.Any(payload => payload.Contains(GuidanceButtons.SuggestionUnavailable, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_clarification_notice() => Payloads.Any(payload => payload.Contains(GuidanceButtons.ClarificationUnavailable, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_feedback_notice() => Payloads.Any(payload => payload.Contains(Feedback.Acknowledgement, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_run_the_sdk_result_handler_for_each_click() => Results.ReceivedCalls().Count().ShouldEqual(3);
    [Fact] void should_execute_successfully() => Results.ReceivedCalls().Select(call => call.GetArguments()[0]).ShouldEachConformTo(result => result is IExecutionResult && result is not IFailResult);
    [Fact] void should_record_feedback() => Interactions.Received(1).RecordFeedback(42, FeedbackVerdict.Up, Arg.Any<CancellationToken>());
}
