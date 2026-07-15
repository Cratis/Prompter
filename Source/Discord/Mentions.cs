// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers questions asked by mentioning the bot in a channel it can read.
/// </summary>
/// <param name="client">The gateway client, used to know the bot's own identity.</param>
/// <param name="rest">The rest client used for replying.</param>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Mentions(
    GatewayClient client,
    RestClient rest,
    IAnswers answers,
    ILogger<Mentions> logger) : IMessageCreateGatewayHandler
{
    /// <inheritdoc/>
    public async ValueTask HandleAsync(Message arg)
    {
        var message = arg;
        if (message.Author.IsBot)
        {
            return;
        }

        if (!message.Content.Contains($"<@{client.Id}>", StringComparison.Ordinal))
        {
            return;
        }

        var question = message.Content
            .Replace($"<@{client.Id}>", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (question.Length == 0)
        {
            return;
        }

        logger.AnsweringMention(message.Author.Id);

        var answer = await answers.For(new(question), UserHash.For(message.Author.Id), "discord-mention");
        await rest.SendMessageAsync(message.ChannelId, DiscordAnswers.Format(answer));
    }
}
