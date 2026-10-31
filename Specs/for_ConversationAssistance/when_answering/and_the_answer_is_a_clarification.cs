// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationAssistance.when_answering;

public class and_the_answer_is_a_clarification : Specification
{
    readonly IAnswers _answers = Substitute.For<IAnswers>();
    readonly IRequestClassifier _classifier = Substitute.For<IRequestClassifier>();
    readonly IRelatedIssues _related = Substitute.For<IRelatedIssues>();
    ConversationReply _reply = null!;

    void Establish() => _answers.For(Arg.Any<Question>(), "offline", Arg.Any<CancellationToken>())
        .Returns(new Answer("Which?", [], 0.8, false, []) { Clarification = new([InteractionProduct.Chronicle, InteractionProduct.Arc]) });

    async Task Because() => _reply = await new ConversationAssistance(_classifier, _answers, _related, Options.Create(new PrompterOptions())).For(new("hello"), "offline", TestContext.Current.CancellationToken);

    [Fact] void should_keep_the_clarification() => _reply.Answer.Clarification.ShouldNotBeNull();
    [Fact] void should_offer_nothing() => _reply.Offer.ShouldBeEmpty();
    [Fact] void should_find_no_related_issues() => _related.DidNotReceive().FindRelated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
