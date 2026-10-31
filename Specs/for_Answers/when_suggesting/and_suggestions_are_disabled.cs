// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_suggesting;

public class and_suggestions_are_disabled : given.a_grounded_answerer
{
    Answers _custom = null!;

    void Establish()
    {
        Found(Passage(0.8));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"text":"Facts [1].","category":"answered","product":"Chronicle","suggestions":["What is a projection?","What is a projection?","See https://x.test"]}"""))));
        _custom = new(passages, chat, interactions, Options.Create(new PrompterOptions { Answering = { SuggestFollowUps = false } }), NullLogger<Answers>.Instance);
    }

    async Task Because() => result = await _custom.For(new("How does Chronicle store facts?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_have_none() => result.Suggestions.ShouldBeEmpty();
}
