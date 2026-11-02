// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Digest;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Operations;

/// <summary>Claims a verified opened issue before attempting an answer or bounded notification.</summary>
/// <param name="answers">The documentation answers.</param>
/// <param name="issues">The comment destination.</param>
/// <param name="channel">The mention-safe maintainer destination.</param>
/// <param name="deliveries">The durable at-most-once attempt ledger.</param>
/// <param name="options">GitHub policy and notification limits.</param>
/// <param name="clock">The shared deadline clock.</param>
/// <param name="logger">Content-free diagnostics.</param>
public class GitHubWebhook(
    IAnswers answers,
    IIssues issues,
    IMaintainerChannel channel,
    IIssueDeliveries deliveries,
    IOptions<PrompterOptions> options,
    TimeProvider clock,
    ILogger<GitHubWebhook> logger)
{
    readonly BoundedWork _notifications = new(1, options.Value.Protection.WebhookNotificationsPerMinute, clock);

    /// <summary>Attempts each issue once; duplicate or unavailable claims produce no external work.</summary>
    /// <param name="issue">The authenticated opened issue.</param>
    /// <param name="cancellationToken">The overall webhook deadline and shutdown token.</param>
    /// <returns>The bounded handling task.</returns>
    public async Task Handle(OpenedIssue issue, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IssueDeliveries.IsValid(issue.Repository, issue.Number) || !await TryClaim(issue, cancellationToken)) return;
        var github = options.Value.GitHub;
        bool? answered = null;
        if (IssueEvents.ShouldAnswer(issue, github)) answered = await TryAnswer(issue, github, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await TryNotify(issue, answered, github, cancellationToken);
    }

    async Task<bool> TryClaim(OpenedIssue issue, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2), clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            var claimed = await deliveries.Claim(issue.Repository, issue.Number, deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            if (!claimed) logger.IssueDeliveryRepeated();
            return claimed;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.IssueClaimFailed();
            return false;
        }
    }

    async Task<bool> TryAnswer(OpenedIssue issue, GitHubOptions github, CancellationToken cancellationToken)
    {
        try
        {
            var answer = await answers.For(new(issue.AsQuestion), "github-issue", cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (answer.IsRefusal)
            {
                logger.IssueNotAnswerable(issue.Repository, issue.Number);
                return false;
            }
            await issues.Comment(issue.RepositoryName, issue.Number, IssueAnswerComment.For(answer, github.OptOutLabel), cancellationToken);
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A claim is an at-most-once attempt, including an ambiguous comment write.
            logger.AnsweringIssueFailed();
            return false;
        }
    }

    async Task TryNotify(OpenedIssue issue, bool? answered, GitHubOptions github, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (github.NotifyChannelId is not { } channelId) return;
        using var turn = _notifications.TryEnter();
        if (turn is null)
        {
            logger.IssueNotificationLimited();
            return;
        }
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2), clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            await channel.Post(channelId, IssueNotification.For(issue, answered), deadline.Token);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.IssueNotificationFailed();
        }
    }
}
