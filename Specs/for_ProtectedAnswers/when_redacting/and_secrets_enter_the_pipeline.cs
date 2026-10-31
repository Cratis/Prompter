// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedAnswers.when_redacting;

public class and_secrets_enter_the_pipeline : for_PaidWork.given.a_paid_work
{
    readonly string _secret = new string(['g', 'h', 'p', '_']) + new string('a', 36);
    bool _retrievalSafe = true;
    bool _modelSafe = true;
    bool _upstreamNotice;
    bool _notice;
    bool _harmlessNotice;

    async Task Because()
    {
        await using var lease = await Work.BeginInteractive();
        lease.Activate();
        var passages = Substitute.For<IPassages>();
        passages.Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _retrievalSafe &= !call.ArgAt<Question>(0).Value.Contains(_secret, StringComparison.Ordinal);
            return Task.FromResult<IEnumerable<Passage>>([new(new("https://cratis.io/arc/"), "Arc", "Arc", "Safe docs", 1, 0.9)]);
        });
        var chat = Substitute.For<IChatClient>();
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _modelSafe &= call.ArgAt<IEnumerable<ChatMessage>>(0).All(message => !message.Text.Contains(_secret, StringComparison.Ordinal));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "safe answer")));
        });
        var options = Microsoft.Extensions.Options.Options.Create(Options);
        var answers = new ProtectedAnswers(new Answers(passages, chat, Substitute.For<IInteractionLog>(), options, NullLogger<Answers>.Instance), Work, options);
        var question = new Question(_secret)
        {
            Conversation = new([new("user", _secret), new("assistant", _secret)])
        };
        _notice = (await answers.For(question, "spec")).SecretsRedacted;
        _harmlessNotice = (await answers.For(new("safe question"), "spec")).SecretsRedacted;
        _upstreamNotice = (await answers.For(new(SecretRedaction.Marker), "spec")).SecretsRedacted;
    }

    [Fact] void should_never_send_the_value_to_retrieval_or_embeddings() => _retrievalSafe.ShouldBeTrue();
    [Fact] void should_never_send_current_or_history_values_to_the_model() => _modelSafe.ShouldBeTrue();
    [Fact] void should_signal_the_removal() => _notice.ShouldBeTrue();
    [Fact] void should_signal_upstream_redaction() => _upstreamNotice.ShouldBeTrue();
    [Fact] void should_not_signal_harmless_text() => _harmlessNotice.ShouldBeFalse();
}
