// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;

namespace Cratis.Prompter.Discord;

/// <summary>Sends an answer as the followup to an interaction, with its buttons and any separate advice.</summary>
public static class InteractionAnswers
{
    /// <summary>Sends the answer, then the advice when there is any.</summary>
    /// <param name="interaction">The interaction to follow up on.</param>
    /// <param name="reply">The answer and advice.</param>
    /// <param name="actions">The explicit answer actions.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Send(Interaction interaction, ConversationReply reply, AnswerActions actions)
    {
        var followup = SafeMessages.Followup(DiscordAnswers.Format(reply.Answer));
        var rows = AnswerComponents.For(reply.Answer, actions);
        if (rows.Count > 0)
        {
            followup.Components = [.. rows];
        }

        await interaction.SendFollowupMessageAsync(followup);
        var advice = DiscordAnswers.Advice(reply);
        if (advice.Length > 0)
        {
            await interaction.SendFollowupMessageAsync(SafeMessages.Followup(advice));
        }
    }
}
