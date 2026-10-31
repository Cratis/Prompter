// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_AnswerActions.when_deciding;

public class and_actions_are_opt_in : Specification
{
    [Fact] void should_offer_nothing_by_default() => AnswerActions.For(new(2, 3), true, false, false, false, "chronicle").ShouldEqual(new(new(2, 3), false, false, null));
    [Fact] void should_offer_human_if_enabled() => AnswerActions.For(new(2, 3), false, true, false, false, "chronicle").OfferHuman.ShouldBeTrue();
    [Fact] void should_offer_documented_for_a_refusal() => AnswerActions.For(new(2, 3), true, false, true, false, "chronicle").OfferDocumented.ShouldBeTrue();
    [Fact] void should_not_offer_documented_for_an_answer() => AnswerActions.For(new(2, 3), false, false, true, false, "chronicle").OfferDocumented.ShouldBeFalse();
    [Fact] void should_not_forward_a_command() => AnswerActions.For(new(2, 0), true, true, true, true, "chronicle").OfferRequest.ShouldBeFalse();
    [Fact] void should_offer_request_instead_of_documented()
    {
        var actions = AnswerActions.For(new(2, 3), true, false, true, true, "chronicle");
        actions.RequestProduct.ShouldEqual("chronicle");
        actions.OfferDocumented.ShouldBeFalse();
    }
    [Fact] void should_not_request_without_forwards() => AnswerActions.For(new(2, 3), true, false, false, true, "chronicle").OfferRequest.ShouldBeFalse();
}
