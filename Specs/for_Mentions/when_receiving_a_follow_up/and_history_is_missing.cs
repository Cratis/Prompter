// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Routing;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_follow_up;

public class and_history_is_missing : given.an_admitted_message
{
    bool _reservedBeforeHistory;
    bool _reservedBeforeClassification;

    void Establish()
    {
        History.IsPublic(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _reservedBeforeHistory = WorkLease.Current is not null && Ledger.ReceivedCalls().Any();
            return true;
        });
        History.Before(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<HistoryMessage>>(new InvalidOperationException("inaccessible")));
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromCanceled<Answer>(new(true)));
        Classifier.Classify(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _reservedBeforeClassification = WorkLease.Current is not null && Ledger.ReceivedCalls().Any();
            return new Classification(RoutingProduct.Unknown, ClassificationIntent.Question, ClassificationAction.Answer, ConfidenceBucket.Unknown, ClassificationSource.Deterministic, ClassificationFallback.NotConfigured);
        });
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_reserve_before_history() => _reservedBeforeHistory.ShouldBeTrue();
    [Fact] void should_reserve_before_classification() => _reservedBeforeClassification.ShouldBeTrue();
    [Fact] void should_answer_using_the_latest_question_only() => ((Question)Answers.ReceivedCalls().Single().GetArguments()[0]!).Value.ShouldEqual("current question");
    [Fact] void should_expose_a_degraded_context_flag() => ((Question)Answers.ReceivedCalls().Single().GetArguments()[0]!).Conversation.IsDegraded.ShouldBeTrue();
}
