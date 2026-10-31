// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationAssistance.when_answering;

public class and_only_public_hints_are_available : Specification
{
    readonly IAnswers _answers = Substitute.For<IAnswers>();
    readonly IRequestClassifier _classifier = Substitute.For<IRequestClassifier>();
    readonly IRelatedIssues _related = Substitute.For<IRelatedIssues>();
    readonly IPublicRelatedIssues _public = Substitute.For<IPublicRelatedIssues>();
    readonly Question _question = new("Arc broken");
    ConversationReply _reply = null!;
    ConversationReply _failing = null!;

    void Establish()
    {
        _answers.For(_question, "offline", Arg.Any<CancellationToken>()).Returns(new Answer("Documentation answer", [], 1, false, []));
        _classifier.Classify(_question.Value, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(new Classification(new("arc"), ClassificationIntent.Bug, ClassificationAction.OfferIssue, ConfidenceBucket.High, ClassificationSource.Laya, ClassificationFallback.None));
        _related.FindRelated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    async Task Because()
    {
        var options = Options.Create(new PrompterOptions());
        _public.Find(_question.Value, Arg.Any<CancellationToken>()).Returns([new(12, "https://github.com/Cratis/Arc/issues/12", false) { Repository = "Arc" }]);
        _reply = await new ConversationAssistance(_classifier, _answers, _related, options, _public).For(_question, "offline");
        _public.Find(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<IReadOnlyList<RelatedIssue>>(_ => throw new HttpRequestException());
        _failing = await new ConversationAssistance(_classifier, _answers, _related, options, _public).For(_question, "offline");
    }

    [Fact] void should_show_the_public_hint() => _reply.Related.Single().Number.ShouldEqual(12);
    [Fact] void should_keep_the_answer_when_the_lookup_fails() => _failing.Answer.Text.ShouldEqual("Documentation answer");
    [Fact] void should_show_nothing_when_the_lookup_fails() => _failing.Related.Count.ShouldEqual(0);
}
