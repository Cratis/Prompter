// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The <c language="csharp">/ask</c> slash command.
/// </summary>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="options">The Prompter options carrying the rate-limit window, timeout, and reply text.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Ask(
    IAnswers answers,
    Admission admission,
    PaidWork work,
    IOptions<PrompterOptions> options,
    ILogger<Ask> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>
    /// Answers a question about Cratis, grounded in the documentation.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Answering takes several seconds, well beyond Discord's 3-second interaction acknowledgement
    /// window, so the command defers immediately (showing a "thinking…" state) and then delivers the
    /// answer as a followup message carrying the 👍/👎 feedback buttons. The rate limit is checked before
    /// deferring so an over-limit ask replies ephemerally without a public "thinking…" placeholder; any
    /// failure or timeout while answering lands a short apology as the followup so the placeholder never hangs.
    /// </remarks>
    [SlashCommand("ask", "Ask Prompter a question about Cratis")]
    public async Task Handle(string question)
    {
        var channel = Context.Channel.Id;
        var decision = admission.TryEnter(new(Context.Interaction.GuildId, channel, Context.User.Id), question);
        if (!decision.Allowed)
        {
            if (decision.Reply)
            {
                await Refuse();
            }

            return;
        }

        var deferred = false;
        try
        {
            await using var lease = await work.BeginInteractive();
            lease.Activate();
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
            deferred = true;
            var answer = await answers.For(new(question), "discord-ask", lease.Token);
            var content = DiscordAnswers.Format(answer);

            if (answer.InteractionId is { } interactionId)
            {
                await Context.Interaction.SendFollowupMessageAsync(new()
                {
                    Content = content,
                    Components = [FeedbackButtonRow.For(interactionId)]
                });
            }
            else
            {
                await Context.Interaction.SendFollowupMessageAsync(content);
            }
        }
        catch (WorkRefused)
        {
            await RefuseAfterAdmission(deferred);
        }
        catch (Exception exception)
        {
            // Already deferred, so the apology must land as a followup inside the interaction window,
            // resolving the "thinking…" placeholder instead of leaving the user hanging.
            logger.AnswerFailed(exception);
            await TryApologize();
        }
    }

    async Task RefuseAfterAdmission(bool deferred)
    {
        if (!admission.MayReply(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id)))
        {
            return;
        }

        if (deferred)
        {
            await Context.Interaction.SendFollowupMessageAsync(new() { Content = options.Value.Discord.RateLimitedReply, Flags = MessageFlags.Ephemeral });
        }
        else
        {
            await Refuse();
        }
    }

    async Task Refuse() => await Context.Interaction.SendResponseAsync(
        InteractionCallback.Message(new() { Content = options.Value.Discord.RateLimitedReply, Flags = MessageFlags.Ephemeral }));

    async Task TryApologize()
    {
        try
        {
            await Context.Interaction.SendFollowupMessageAsync(options.Value.Discord.ErrorReply);
        }
        catch (Exception exception)
        {
            // The followup itself failed to send; nothing more can be delivered to the interaction, so just
            // record it — the command still returns without throwing.
            logger.ApologyFailed(exception);
        }
    }
}
