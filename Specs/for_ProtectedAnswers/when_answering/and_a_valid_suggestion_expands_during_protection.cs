// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord.Rest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedAnswers.when_answering;

public class and_a_valid_suggestion_expands_during_protection : for_PaidWork.given.a_paid_work
{
    const string Suggestion = "How do I apply [EventType] attributes to event records in a Cratis application?";
    Answer _answer = null!;
    ButtonProperties _button = null!;

    async Task Because()
    {
        var passages = Substitute.For<IPassages>();
        passages.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Passage>>([new(new("https://cratis.io/arc/"), "Arc", "Events", "Events", 0.02, 0.9)]));
        var chat = Substitute.For<IChatClient>();
        var output = JsonSerializer.Serialize(new { text = "Use the documented attributes [1].", suggestions = new[] { Suggestion } });
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, output))));
        Options.Answering.SuggestFollowUps = true;
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var inner = new Answers(passages, chat, Substitute.For<IInteractionLog>(), options, NullLogger<Answers>.Instance);
        _answer = await new ProtectedAnswers(inner, Work, options).For(new("How are events modeled?"), "spec");
        _button = AnswerComponents.For(_answer, AnswerActions.None).SelectMany(row => row.Components).OfType<ButtonProperties>().Single(button => button.CustomId == SuggestionButton.CustomId(0));
    }

    [Fact] void should_keep_the_safe_underlying_question() => _answer.Suggestions.Single().ShouldEqual(PublishedMarkdown.Neutralize(Suggestion, Options));
    [Fact] void should_use_the_original_plain_question_as_the_display_label() => _button.Label.ShouldEqual(Suggestion);
    [Fact] void should_validate_the_final_label_length() => (_button.Label?.Length is > 0 and <= 80).ShouldBeTrue();
    [Fact] void should_preserve_the_original_question_index() => _button.CustomId.ShouldEqual(SuggestionButton.CustomId(0));
}
