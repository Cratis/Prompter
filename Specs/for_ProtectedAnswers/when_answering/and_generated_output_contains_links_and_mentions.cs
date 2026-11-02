// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedAnswers.when_answering;

public class and_generated_output_contains_links_and_mentions : for_PaidWork.given.a_paid_work
{
    static readonly string[] _suggestions = ["Explain [Guide](javascript:alert)?"];
    Answer _result = null!;
    Answer _alreadyLeased = null!;

    async Task Because()
    {
        var passages = Substitute.For<IPassages>();
        passages.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Passage>>([new(new("https://cratis.io/arc/"), "Arc", "Commands", "Commands", 0.02, 0.9)]));
        var chat = Substitute.For<IChatClient>();
        var output = JsonSerializer.Serialize(new { text = "Ask @someone at [Guide](https://elsewhere.invalid) [1].", suggestions = _suggestions });
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, output))));
        Options.Answering.SuggestFollowUps = true;
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var inner = new Answers(passages, chat, Substitute.For<IInteractionLog>(), options, NullLogger<Answers>.Instance);
        var answers = new ProtectedAnswers(inner, Work, options);
        _result = await answers.For(new("Commands"), "spec");
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        _alreadyLeased = await answers.For(new("Commands"), "spec", TestContext.Current.CancellationToken);
    }

    [Fact] void should_protect_the_already_leased_path_identically() => _alreadyLeased.Text.ShouldEqual(_result.Text);
    [Fact] void should_protect_already_leased_suggestions() => _alreadyLeased.Suggestions.Single().ShouldEqual(_result.Suggestions.Single());
    [Fact] void should_not_create_another_lease_for_the_admitted_path() => Ledger.Received(2).Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>());
    [Fact] void should_apply_policy_to_the_answer() => _result.Text.ShouldEqual("Ask `@someone` at Guide `https://elsewhere.invalid` [1].");
    [Fact] void should_apply_policy_to_the_follow_up() => _result.Suggestions.Single().ShouldEqual("Explain Guide `javascript:alert`?");
    [Fact] void should_keep_citation_metadata() => _result.Citations.Single().Value.ShouldEqual("https://cratis.io/arc/");
}
