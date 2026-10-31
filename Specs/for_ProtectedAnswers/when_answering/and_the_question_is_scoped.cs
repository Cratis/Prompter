// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedAnswers.when_answering;

public class and_the_question_is_scoped : for_PaidWork.given.a_paid_work
{
    Question? _searched;

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        var passages = Substitute.For<IPassages>();
        passages.Search(Arg.Do<Question>(question => _searched = question), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<Passage>>([]));
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var inner = new Answers(passages, Substitute.For<IChatClient>(), Substitute.For<IInteractionLog>(), options, NullLogger<Answers>.Instance);
        var answers = new ProtectedAnswers(inner, Work, options);
        await answers.For(new("hello") { Scope = InteractionProduct.Arc }, "spec", TestContext.Current.CancellationToken);
    }

    [Fact] void should_pass_the_scope_to_retrieval() => _searched!.Scope.ShouldEqual(InteractionProduct.Arc);
}
