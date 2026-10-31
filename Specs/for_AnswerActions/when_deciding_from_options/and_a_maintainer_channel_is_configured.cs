// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_AnswerActions.when_deciding_from_options;

public class and_a_maintainer_channel_is_configured : Specification
{
    DiscordOptions _options = null!;
    AnswerActions _refusal = null!;
    AnswerActions _docsGap = null!;
    AnswerActions _answered = null!;
    AnswerActions _unconfigured = null!;

    static ConversationReply Reply(bool refusal, InteractionCategory category) =>
        new(new Answer("text", [], 1, refusal, []) { Category = category }, [], string.Empty);

    void Establish() => _options = new() { MaintainerChannelId = 9 };

    void Because()
    {
        _refusal = AnswerActions.For(_options, new(2, 3), Reply(true, InteractionCategory.Unknown));
        _docsGap = AnswerActions.For(_options, new(2, 3), Reply(false, InteractionCategory.DocsGap));
        _answered = AnswerActions.For(_options, new(2, 3), Reply(false, InteractionCategory.Answered));
        _options.MaintainerChannelId = null;
        _unconfigured = AnswerActions.For(_options, new(2, 3), Reply(true, InteractionCategory.Unknown));
    }

    [Fact] void should_offer_documented_on_a_refusal() => _refusal.OfferDocumented.ShouldBeTrue();
    [Fact] void should_offer_documented_on_a_docs_gap() => _docsGap.OfferDocumented.ShouldBeTrue();
    [Fact] void should_not_offer_documented_on_an_answer() => _answered.OfferDocumented.ShouldBeFalse();
    [Fact] void should_offer_nothing_without_a_channel() => _unconfigured.OfferDocumented.ShouldBeFalse();
}
