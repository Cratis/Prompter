// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedAnswers.when_answering;

public class and_admission_fails : Specification
{
    IPassages _passages = null!;
    IChatClient _chat = null!;
    IInteractionLog _log = null!;
    ProtectedAnswers _answers = null!;
    PaidWork _work = null!;
    Exception? _error;

    void Establish()
    {
        _passages = Substitute.For<IPassages>();
        _chat = Substitute.For<IChatClient>();
        _log = Substitute.For<IInteractionLog>();
        var options = Options.Create(new PrompterOptions());
        _work = new(Substitute.For<ISpendLedger>(), options, TimeProvider.System);
        var inner = new Answers(_passages, _chat, _log, options, NullLogger<Answers>.Instance);
        _answers = new(inner, _work, options);
    }

    async Task Because() => _error = await Catch.Exception(() => _answers.For(new("hello"), "spec"));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_retrieve_or_embed() => _passages.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_call_anthropic() => _chat.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_record_an_interaction() => _log.ReceivedCalls().ShouldBeEmpty();

    void Destroy() => _work.Dispose();
}
