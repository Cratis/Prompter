// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers posts in opted-in forums that nobody has answered after their delay. Nothing about a post is persisted:
/// Discord's own messages say whether Prompter already answered, and a bounded in-memory set of thread identifiers
/// prevents a retry loop within one process.
/// </summary>
/// <param name="threads">Reads forum post metadata.</param>
/// <param name="answerer">The shared forum answer path.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="options">The Prompter options carrying the delayed forums.</param>
/// <param name="time">The UTC clock and timer.</param>
/// <param name="logger">The logger.</param>
public class DelayedForumAnswers(
    IForumThreads threads,
    IForumAnswerer answerer,
    Admission admission,
    IOptions<PrompterOptions> options,
    TimeProvider time,
    ILogger<DelayedForumAnswers> logger) : BackgroundService
{
    /// <summary>The most posts answered in one sweep, bounding a catch-up burst.</summary>
    public const int MaximumAnswersPerSweep = 3;

    /// <summary>How often the opted-in forums are swept.</summary>
    public static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    readonly Dictionary<ulong, DateTimeOffset> _settled = [];

    /// <summary>
    /// Sweeps every opted-in forum once and answers due, unanswered posts, oldest first.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The number of posts answered.</returns>
    public async Task<int> SweepOnce(CancellationToken ct)
    {
        var forums = DelayedForumSchedule.Configured(options.Value.Discord);
        if (forums.Count == 0 || !options.Value.Protection.AnsweringEnabled)
        {
            return 0;
        }

        Prune();
        var answered = 0;
        foreach (var forum in forums)
        {
            try
            {
                answered += await SweepForum(forum, MaximumAnswersPerSweep - answered, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Do not log exception text; only the forum identifier.
                logger.SweepFailed(forum.ChannelId);
            }

            if (answered >= MaximumAnswersPerSweep)
            {
                break;
            }
        }

        return answered;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (DelayedForumSchedule.Configured(options.Value.Discord).Count == 0)
        {
            logger.Disabled();
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(SweepInterval, time, stoppingToken);
                try
                {
                    await SweepOnce(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.SweepsFailed();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown cancels the timer and any in-flight sweep.
        }
    }

    async Task<int> SweepForum(DelayedForum forum, int budget, CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var due = (await threads.Active(forum.ChannelId, ct))
            .Where(thread => DelayedForumSchedule.IsDue(thread, forum.Delay, now) && !_settled.ContainsKey(thread.Id))
            .OrderBy(thread => thread.CreatedAt);
        var answered = 0;
        foreach (var thread in due)
        {
            if (answered >= budget)
            {
                break;
            }

            if (!InScope(thread, forum) || await Reply(thread, forum, ct) != ForumReplyState.Unanswered)
            {
                continue;
            }

            logger.AnsweringDelayedThread(thread.Id);
            await answerer.Answer(new(thread.GuildId, thread.Id, thread.ParentId, thread.OwnerId), forum.ChannelId, null, delayed: true);
            answered++;
        }

        return answered;
    }

    bool InScope(ForumThreadSnapshot thread, DelayedForum forum) =>
        admission.InScope(thread.GuildId, forum.ChannelId) && admission.InScope(thread.GuildId, thread.Id);

    async Task<ForumReplyState> Reply(ForumThreadSnapshot thread, DelayedForum forum, CancellationToken ct)
    {
        var recent = await threads.Recent(thread.Id, DelayedForumSchedule.ReplyScanLimit, ct);
        var state = DelayedForumSchedule.Replies(recent, thread.Id, thread.OwnerId, await threads.BotId(ct));
        Settle(thread.Id, thread.CreatedAt + forum.Delay + DelayedForumSchedule.MaximumLateness);
        return state;
    }

    void Settle(ulong threadId, DateTimeOffset expires)
    {
        if (_settled.Count >= options.Value.Protection.LocalCapacity && _settled.Count > 0)
        {
            _settled.Remove(_settled.MinBy(entry => entry.Value).Key);
        }

        _settled[threadId] = expires;
    }

    void Prune()
    {
        var now = time.GetUtcNow();
        foreach (var expired in _settled.Where(entry => entry.Value <= now).Select(entry => entry.Key).ToList())
        {
            _settled.Remove(expired);
        }
    }
}
