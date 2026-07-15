// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The <c>/ask</c> slash command.
/// </summary>
/// <param name="answers">The answers Prompter can give.</param>
public class Ask(IAnswers answers) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>
    /// Answers a question about Cratis, grounded in the documentation.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Answering takes several seconds, well beyond Discord's 3-second interaction acknowledgement
    /// window, so the command defers immediately (showing a "thinking…" state) and then delivers the
    /// answer as a followup message.
    /// </remarks>
    [SlashCommand("ask", "Ask Prompter a question about Cratis")]
    public async Task Handle(string question)
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        var answer = await answers.For(new(question), UserHash.For(Context.User.Id), "discord-ask");

        await Context.Interaction.SendFollowupMessageAsync(DiscordAnswers.Format(answer));
    }
}
