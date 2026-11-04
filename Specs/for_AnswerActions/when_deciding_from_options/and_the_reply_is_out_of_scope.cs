// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_AnswerActions.when_deciding_from_options;

public class and_the_reply_is_out_of_scope : Specification
{
    AnswerActions _outOfScope = null!;
    AnswerActions _floorRefusal = null!;

    void Because()
    {
        var options = new DiscordOptions { MaintainerChannelId = 9 };
        _outOfScope = AnswerActions.For(options, new(2, 3), new ConversationReply(Answer.OutOfScope(0.55, [], InteractionProduct.OtherOrUnknown), [], string.Empty));
        _floorRefusal = AnswerActions.For(options, new(2, 3), new ConversationReply(Answer.Refusal(0.3, []), [], string.Empty));
    }

    [Fact] void should_not_offer_documented_for_out_of_scope() => _outOfScope.OfferDocumented.ShouldBeFalse();
    [Fact] void should_still_offer_documented_for_a_floor_refusal() => _floorRefusal.OfferDocumented.ShouldBeTrue();
}
