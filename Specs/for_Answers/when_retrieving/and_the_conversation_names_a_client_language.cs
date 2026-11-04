// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_the_conversation_names_a_client_language : given.a_grounded_answerer
{
    readonly ConversationContext _conversation = new([new("user", "I am building with Elixir")]);

    void Establish() => Found(Passage(0.9));

    async Task Because() => result = await answers.For(new("and how about concurrency?") { Conversation = _conversation }, "spec", TestContext.Current.CancellationToken);

    [Fact] void should_pass_the_named_language_to_retrieval() => passages.Received(1).Search(Arg.Is<Question>(question => question.ClientLanguages == ClientLanguage.Elixir), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_pass_the_conversation_to_retrieval() => passages.Received(1).Search(Arg.Is<Question>(question => question.Conversation.Messages.Count == 0), Arg.Any<int>(), Arg.Any<CancellationToken>());
}
