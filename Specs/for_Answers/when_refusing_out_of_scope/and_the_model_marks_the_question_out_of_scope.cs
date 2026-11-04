// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_refusing_out_of_scope;

public class and_the_model_marks_the_question_out_of_scope : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.55));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"text":"Use flexbox [1].","category":"out-of-scope","product":"other/unknown","suggestions":["More CSS?"]}"""))));
    }

    async Task Because() => result = await answers.For(new("How do I center a div?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_cite() => result.Citations.ShouldBeEmpty();
    [Fact] void should_not_suggest() => result.Suggestions.ShouldBeEmpty();
    [Fact] void should_use_the_out_of_scope_text() => result.Text.ShouldEqual(Answer.OutOfScopeText);
    [Fact] void should_not_contain_the_generated_text() => result.Text.ShouldNotContain("flexbox");
    [Fact] void should_expose_the_category() => result.Category.ShouldEqual(InteractionCategory.OutOfScope);
    [Fact] void should_generate_exactly_once() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_record_a_refusal() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => interaction.WasRefusal && !interaction.CitedPages.Any() && interaction.Category == InteractionCategory.OutOfScope), Arg.Any<CancellationToken>());
}
