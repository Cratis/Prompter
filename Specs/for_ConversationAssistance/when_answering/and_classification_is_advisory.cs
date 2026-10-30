// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationAssistance.when_answering;

public class and_classification_is_advisory : Specification
{
    readonly IAnswers _answers = Substitute.For<IAnswers>();
    readonly IRequestClassifier _classifier = Substitute.For<IRequestClassifier>();
    readonly IRelatedIssues _related = Substitute.For<IRelatedIssues>();
    readonly Answer _answer = new("Documentation answer", [], 1, false, []) { InteractionId = 42 };
    readonly Question _question = new("Arc broken") { Conversation = new([new("user", "earlier"), new("assistant", "untrusted bot output")]) };
    ConversationReply _reply = null!;

    void Establish()
    {
        _answers.For(_question, "offline", Arg.Any<CancellationToken>()).Returns(_answer);
        _classifier.Classify(_question.Value, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(new Classification(new("arc"), ClassificationIntent.Bug, ClassificationAction.OfferIssue, ConfidenceBucket.High, ClassificationSource.Laya, ClassificationFallback.None));
        _related.FindRelated("arc", _question.Value, Arg.Any<CancellationToken>()).Returns([new(12, "https://github.com/Cratis/Arc/issues/12", false)]);
    }

    async Task Because()
    {
        var options = Options.Create(new PrompterOptions());
        options.Value.Protection.GitHubWritesEnabled = true;
        options.Value.GitHub.InstallationRepositories = ["Cratis/Arc"];
        options.Value.GitHub.AllowTokenFallback = true;
        options.Value.GitHub.Token = "offline-placeholder";
        _reply = await new ConversationAssistance(_classifier, _answers, _related, options).For(_question, "offline");
    }

    [Fact] void should_preserve_the_exact_documentation_answer() => ReferenceEquals(_reply.Answer, _answer).ShouldBeTrue();
    [Fact] void should_keep_related_advice_separate() => _reply.Related.Count.ShouldEqual(1);
    [Fact] void should_route_only_to_explicit_issue_command() => _reply.Offer.Contains("/issue", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_send_bot_output_to_the_classifier() => _classifier.Received().Classify(_question.Value, Arg.Is<IReadOnlyList<string>>(history => history.Count == 1 && history[0] == "earlier"), Arg.Any<CancellationToken>());
    [Fact] void should_not_change_the_documentation_prompt() => _answers.Received(1).For(_question, "offline", Arg.Any<CancellationToken>());
}
