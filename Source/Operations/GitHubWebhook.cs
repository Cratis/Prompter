// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Operations;

/// <summary>
/// Handles a verified GitHub webhook delivery: answers a newly-opened issue from the documentation when it
/// can, and tells the maintainer channel either way.
/// </summary>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="issues">Posts the answer back as a comment.</param>
/// <param name="gateway">The gateway client, used for its REST access to the maintainer channel.</param>
/// <param name="options">The Prompter options carrying the GitHub configuration.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// The rule that matters here is <em>silence on refusal</em>. A tracker comment is the most visible thing
/// this bot does, and a hedging "I'm not sure, but…" on someone's bug report costs a maintainer more
/// attention than it saves. So an ungrounded answer is never posted — but it is still reported to the
/// maintainer channel, because "nothing in the docs covers this" is exactly the signal worth having.
/// </remarks>
public class GitHubWebhook(
    IAnswers answers,
    IIssues issues,
    GatewayClient gateway,
    IOptions<PrompterOptions> options,
    ILogger<GitHubWebhook> logger)
{
    /// <summary>
    /// Processes a newly-opened issue.
    /// </summary>
    /// <param name="issue">The opened issue.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Handle(OpenedIssue issue, CancellationToken cancellationToken)
    {
        var github = options.Value.GitHub;
        bool? answered = null;

        if (IssueEvents.ShouldAnswer(issue, github))
        {
            answered = await TryAnswer(issue, github, cancellationToken);
        }

        await TryNotify(issue, answered, github, cancellationToken);
    }

    async Task<bool> TryAnswer(OpenedIssue issue, GitHubOptions github, CancellationToken cancellationToken)
    {
        try
        {
            var answer = await answers.For(new(issue.AsQuestion), "github-issue", cancellationToken);

            if (answer.IsRefusal)
            {
                logger.IssueNotAnswerable(issue.Repository, issue.Number);
                return false;
            }

            await issues.Comment(
                issue.RepositoryName,
                issue.Number,
                IssueAnswerComment.For(answer, github.OptOutLabel),
                cancellationToken);

            return true;
        }
        catch (Exception exception)
        {
            // The delivery was already accepted; a failure answering must not surface as a failed webhook,
            // which would make GitHub retry and risk duplicate comments.
            logger.AnsweringIssueFailed(exception, issue.Repository, issue.Number);

            return false;
        }
    }

    async Task TryNotify(OpenedIssue issue, bool? answered, GitHubOptions github, CancellationToken cancellationToken)
    {
        if (github.NotifyChannelId is not { } channelId)
        {
            return;
        }

        try
        {
            await gateway.Rest.SendMessageAsync(
                channelId,
                new MessageProperties { Content = IssueNotification.For(issue, answered) },
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            // A missed announcement is not worth failing a delivery over.
            logger.IssueNotificationFailed(exception, issue.Repository, issue.Number);
        }
    }
}
