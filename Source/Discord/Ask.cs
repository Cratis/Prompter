// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
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
    /// <returns>The formatted answer.</returns>
    [SlashCommand("ask", "Ask Prompter a question about Cratis")]
    public async Task<string> Handle(string question)
    {
        var answer = await answers.For(new(question), UserHash.For(Context.User.Id), "discord-ask");

        return DiscordAnswers.Format(answer);
    }
}
