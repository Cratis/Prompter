// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.given;

public class a_grounded_answerer : Specification
{
    protected const string Documentation = "Cratis Chronicle stores immutable facts and builds read models from events.";
    protected IPassages passages { get; private set; } = null!;
    protected IChatClient chat { get; private set; } = null!;
    protected IInteractionLog interactions { get; private set; } = null!;
    protected Answers answers { get; private set; } = null!;
    protected IReadOnlyList<ChatMessage> sent { get; private set; } = [];
    protected Answer result { get; set; } = null!;

    protected static Passage Passage(double similarity, double rank = 1.0 / 61) => new(
        new("https://cratis.io/chronicle/"), "Chronicle", "Overview", Documentation, rank, similarity);

    protected void Found(params Passage[] found) => passages.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<IEnumerable<Passage>>(found));

    void Establish()
    {
        passages = Substitute.For<IPassages>();
        chat = Substitute.For<IChatClient>();
        interactions = Substitute.For<IInteractionLog>();
        interactions.Record(Arg.Any<Interaction>(), Arg.Any<CancellationToken>()).Returns(42L);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            sent = call.Arg<IEnumerable<ChatMessage>>().ToArray();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Chronicle stores facts [1].")));
        });
        answers = new(passages, chat, interactions, Options.Create(new PrompterOptions()), NullLogger<Answers>.Instance);
    }
}
