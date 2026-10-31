// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_AnswerActions.when_deciding_from_options;

public class and_the_reply_is_a_missing_capability : Specification
{
    AnswerActions _actions = null!;
    AnswerActions _unconfigured = null!;

    void Because()
    {
        var reply = new ConversationReply(new Answer("text", [], 1, true, []) { Category = InteractionCategory.MissingCapability, Product = InteractionProduct.Chronicle }, [], string.Empty);
        _actions = AnswerActions.For(new() { MaintainerChannelId = 9 }, new(2, 3), reply);
        _unconfigured = AnswerActions.For(new(), new(2, 3), reply);
    }

    [Fact] void should_request_with_the_product() => _actions.RequestProduct.ShouldEqual("chronicle");
    [Fact] void should_suppress_documented() => _actions.OfferDocumented.ShouldBeFalse();
    [Fact] void should_not_request_without_a_channel() => _unconfigured.OfferRequest.ShouldBeFalse();
}
