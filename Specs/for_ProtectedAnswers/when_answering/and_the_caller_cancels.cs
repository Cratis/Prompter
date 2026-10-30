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

public class and_the_caller_cancels : for_PaidWork.given.a_paid_work
{
    bool _propagated;

    async Task Because()
    {
        using var caller = new CancellationTokenSource();
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        var passages = Substitute.For<IPassages>();
        var completion = new TaskCompletionSource<IEnumerable<Passage>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = CancellationToken.None;
        passages.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Do<CancellationToken>(token => observed = token)).Returns(completion.Task);
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var inner = new Answers(passages, Substitute.For<IChatClient>(), Substitute.For<IInteractionLog>(), options, NullLogger<Answers>.Instance);
        var answers = new ProtectedAnswers(inner, Work, options);
        var pending = answers.For(new("hello"), "spec", caller.Token);
        await caller.CancelAsync();
        _propagated = observed.IsCancellationRequested;
        completion.SetResult([]);
        await pending;
    }

    [Fact] void should_preserve_the_callers_cancellation_within_an_active_lease() => _propagated.ShouldBeTrue();
}
