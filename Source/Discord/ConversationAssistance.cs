// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>Advisory orchestration, called only after public-scope admission and lease activation.</summary>
/// <param name="classifier">The bounded private classifier or deterministic fallback.</param>
/// <param name="answers">The unchanged protected documentation pipeline.</param>
/// <param name="related">GET-only hints, with no write collaborator.</param>
/// <param name="options">The filing kill switch and configured routing.</param>
/// <param name="publicRelated">Optional anonymous public hints, used only when no authenticated hints are available.</param>
public sealed class ConversationAssistance(IRequestClassifier classifier, IAnswers answers, IRelatedIssues related, IOptions<PrompterOptions> options, IPublicRelatedIssues? publicRelated = null)
{
    /// <summary>Answers first-class documentation questions without treating model data as authority.</summary>
    /// <param name="question">The request with existing sanitized, scope-bounded context.</param>
    /// <param name="source">The unchanged accounting source.</param>
    /// <param name="cancellationToken">The active lease cancellation.</param>
    /// <returns>The original answer and optional separate advice.</returns>
    public async Task<ConversationReply> For(Question question, string source, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var classification = await Classify(question, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var answer = await answers.For(question, source, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (classification is null || !IssueRouting.IsRouted(classification.Product.Value, options.Value.GitHub))
        {
            return new(answer, [], string.Empty);
        }
        var hints = await Safely(() => related.FindRelated(classification.Product.Value, ConversationText.Sanitize(question.Value), cancellationToken), cancellationToken);
        if (hints.Count == 0 && publicRelated is not null)
        {
            hints = await Safely(() => publicRelated.Find(ConversationText.Sanitize(question.Value), cancellationToken), cancellationToken);
        }

        return new(answer, hints, Offer(classification));
    }

    static async Task<IReadOnlyList<RelatedIssue>> Safely(Func<Task<IReadOnlyList<RelatedIssue>>> lookup, CancellationToken cancellationToken)
    {
        try
        {
            return await lookup();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
    }

    static bool SupportsOffer(Classification classification) => classification.Action == ClassificationAction.OfferIssue &&
        classification.Confidence == ConfidenceBucket.High && classification.Fallback == ClassificationFallback.None;

    async Task<Classification?> Classify(Question question, CancellationToken cancellationToken)
    {
        try
        {
            var older = question.Conversation.Messages.Where(message => message.Role == "user")
                .Select(message => message.Content).ToArray();
            return await classifier.Classify(ConversationText.Sanitize(question.Value), older, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    bool FilingEnabled(string product)
    {
        if (!options.Value.Protection.GitHubWritesEnabled || !options.Value.GitHub.FilingEnabled) return false;
        var github = options.Value.GitHub;
        var repository = github.Repositories[product];
        return github.InstallationRepositories.Contains($"{github.Owner}/{repository}", StringComparer.OrdinalIgnoreCase);
    }

    string Offer(Classification classification)
    {
        if (!SupportsOffer(classification) || !FilingEnabled(classification.Product.Value))
        {
            return string.Empty;
        }
        return classification.Intent switch
        {
            ClassificationIntent.Bug => "I can help draft a bug report. Use `/issue` to review a private preview and explicitly confirm before anything is filed.",
            ClassificationIntent.Feature or ClassificationIntent.Idea => "I can help draft a feature request. Use `/issue` to review a private preview and explicitly confirm before anything is filed.",
            _ => string.Empty
        };
    }
}
