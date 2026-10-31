// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationAssistance.when_answering;

public class and_advice_cannot_authorize_actions : Specification
{
    readonly IAnswers _answers = Substitute.For<IAnswers>();
    readonly IRequestClassifier _classifier = Substitute.For<IRequestClassifier>();
    readonly IRelatedIssues _related = Substitute.For<IRelatedIssues>();
    readonly Answer _answer = Answer.Refusal(0, []);
    readonly PrompterOptions _options = new();
    readonly Classification _classification = new(new("arc"), ClassificationIntent.Bug, ClassificationAction.OfferIssue, ConfidenceBucket.High, ClassificationSource.Laya, ClassificationFallback.None);
    ConversationAssistance _assistance = null!;

    void Establish()
    {
        _options.Protection.GitHubWritesEnabled = true;
        _options.GitHub.InstallationRepositories = ["Cratis/Arc"];
        _options.GitHub.AllowTokenFallback = true;
        _options.GitHub.Token = "offline-placeholder";
        _answers.For(Arg.Any<Question>(), "offline", Arg.Any<CancellationToken>()).Returns(_answer);
        _related.FindRelated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        _assistance = new(_classifier, _answers, _related, Options.Create(_options));
    }

    [Theory]
    [InlineData(ClassificationIntent.Bug, true)]
    [InlineData(ClassificationIntent.Feature, true)]
    [InlineData(ClassificationIntent.Idea, true)]
    [InlineData(ClassificationIntent.Question, false)]
    [InlineData(ClassificationIntent.Documentation, false)]
    [InlineData((ClassificationIntent)99, false)]
    async Task should_offer_only_a_supported_kind(ClassificationIntent intent, bool expected)
    {
        Configure(_classification with { Intent = intent });
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        (reply.Offer.Length > 0).ShouldEqual(expected);
        ReferenceEquals(reply.Answer, _answer).ShouldBeTrue();
    }

    [Fact]
    async Task should_not_offer_with_writes_disabled()
    {
        Configure(_classification);
        _options.Protection.GitHubWritesEnabled = false;
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
    }

    [Fact]
    async Task should_not_offer_with_missing_credentials()
    {
        Configure(_classification);
        _options.GitHub.Token = string.Empty;
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
    }

    [Fact]
    async Task should_not_offer_outside_the_installation()
    {
        Configure(_classification);
        _options.GitHub.InstallationRepositories = ["Cratis/Other"];
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
    }

    [Fact]
    async Task should_not_offer_for_low_confidence()
    {
        Configure(_classification with { Confidence = ConfidenceBucket.Low });
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
    }

    [Fact]
    async Task should_not_offer_for_unknown_product()
    {
        Configure(_classification with { Product = RoutingProduct.Unknown });
        var reply = await _assistance.For(new("unsure"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
        _related.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    async Task should_not_offer_from_fallback_data()
    {
        Configure(_classification with { Fallback = ClassificationFallback.Shadow });
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        reply.Offer.ShouldEqual(string.Empty);
    }

    [Fact]
    async Task should_preserve_documentation_if_classification_fails()
    {
        _classifier.Classify(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Classification>(new HttpRequestException("offline")));
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        ReferenceEquals(reply.Answer, _answer).ShouldBeTrue();
        reply.Related.ShouldBeEmpty();
        _related.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    async Task should_preserve_documentation_if_search_fails()
    {
        Configure(_classification);
        _related.FindRelated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<RelatedIssue>>(new HttpRequestException("offline")));
        var reply = await _assistance.For(new("Arc broken"), "offline", TestContext.Current.CancellationToken);
        ReferenceEquals(reply.Answer, _answer).ShouldBeTrue();
        reply.Related.ShouldBeEmpty();
    }

    [Fact]
    async Task should_propagate_caller_cancellation()
    {
        Configure(_classification);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _assistance.For(new("Arc broken"), "offline", cancellation.Token));
        _related.ReceivedCalls().ShouldBeEmpty();
    }

    void Configure(Classification classification) => _classifier.Classify(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>()).Returns(classification);
}
