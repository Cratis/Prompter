// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>Bounds the entire best-effort feedback lifecycle before any transport or storage work.</summary>
/// <param name="votes">Short-lived user quotas and serialized verdict reservations.</param>
/// <param name="interactionLog">The persisted feedback destination.</param>
/// <param name="options">Feedback work limits.</param>
/// <param name="clock">The deadline clock.</param>
/// <param name="logger">Content-free diagnostics.</param>
public sealed class FeedbackProcessing(FeedbackVotes votes, IInteractionLog interactionLog, IOptions<PrompterOptions> options, TimeProvider clock, ILogger<FeedbackProcessing> logger)
{
    readonly BoundedWork _work = new(options.Value.Protection.FeedbackConcurrency, options.Value.Protection.FeedbackPerMinute, clock);

    /// <summary>Processes an admitted click under one shared deadline, including acknowledgement and persistence.</summary>
    /// <param name="userId">The clicking user, retained only as a short-lived keyed hash.</param>
    /// <param name="customId">The feedback button identifier.</param>
    /// <param name="callbacks">The deadline-aware transport callbacks.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The bounded lifecycle, or immediate completion on global denial.</returns>
    public async Task Run(ulong userId, string customId, FeedbackCallbacks callbacks, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return;
        using var work = _work.TryEnter();
        if (work is null) return;
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.Value.Protection.FeedbackWriteTimeoutMilliseconds), clock);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            await Process(userId, customId, callbacks, deadline.Token);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            logger.FeedbackProcessingCanceled();
        }
    }

    async Task Process(ulong userId, string customId, FeedbackCallbacks callbacks, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!votes.TrySpend(userId))
        {
            await callbacks.Refuse(cancellationToken);
            return;
        }
        var click = FeedbackButton.Parse(customId);
        if (click is null)
        {
            await callbacks.Acknowledge(cancellationToken);
            logger.UnrecognizedFeedbackCustomId();
            return;
        }
        using var turn = await votes.Enter(click.InteractionId, userId, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (turn is null)
        {
            await callbacks.Refuse(cancellationToken);
            return;
        }
        switch (votes.Check(click))
        {
            case FeedbackVote.Repeat:
                await callbacks.Repeat(cancellationToken);
                logger.FeedbackRepeated();
                return;
            case FeedbackVote.Refused:
                await callbacks.Refuse(cancellationToken);
                return;
        }
        await callbacks.Acknowledge(cancellationToken);
        await Record(click, cancellationToken);
    }

    async Task Record(FeedbackClick click, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await interactionLog.RecordFeedback(click.InteractionId, click.Verdict, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            votes.Commit(click);
            logger.RecordedFeedback();
        }
        catch (Exception)
        {
            // Feedback remains best-effort after acknowledgement; an uncommitted reservation is released.
            logger.FeedbackWriteFailed();
        }
    }
}
