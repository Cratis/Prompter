// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Anthropic.Models.Messages;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents an implementation of <see cref="IAnswers"/> using hybrid retrieval and a chat model.
/// </summary>
/// <param name="passages">The retrievable passages.</param>
/// <param name="chatClient">The chat model used for generation.</param>
/// <param name="interactionLog">The log interactions are recorded in.</param>
/// <param name="options">The Prompter options.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Answers(
    IPassages passages,
    IChatClient chatClient,
    IInteractionLog interactionLog,
    IOptions<PrompterOptions> options,
    ILogger<Answers> logger) : IAnswers
{
    /// <inheritdoc/>
    public async Task<Answer> For(Question question, string source, CancellationToken cancellationToken = default)
    {
        var answering = options.Value.Answering;
        var found = (await passages.Search(question, answering.MaxPassages, cancellationToken)).ToArray();
        var confidence = found.Length == 0 ? 0 : found[0].Score;

        Answer answer;
        if (confidence < answering.MinScore)
        {
            logger.RefusingToAnswer(confidence);
            answer = Answer.Refusal(confidence, found);
        }
        else
        {
            // The system prompt is a frozen constant, so mark it as an Anthropic prompt-cache
            // breakpoint (D-5). The AsIChatClient adapter maps this to cache_control:
            // { type: "ephemeral" } on the system block, letting the cached prefix be reused
            // across questions once it exceeds the model's minimum cacheable length.
            var systemMessage = new ChatMessage(
                ChatRole.System,
                [new TextContent(SystemPrompt.Text).WithCacheControl(new CacheControlEphemeral())]);

            var response = await chatClient.GetResponseAsync(
                [
                    systemMessage,
                    new(ChatRole.User, ComposeUserMessage(question, found))
                ],
                new() { MaxOutputTokens = answering.MaxOutputTokens },
                cancellationToken);

            var citations = found.Select(passage => passage.Page).Distinct().Take(4).ToArray();
            answer = new(response.Text, citations, confidence, IsRefusal: false, found);
        }

        var interactionId = await interactionLog.Record(
            new(source, answer.Citations, answer.Confidence, answer.IsRefusal),
            cancellationToken);

        return answer with { InteractionId = interactionId };
    }

    static string ComposeUserMessage(Question question, IEnumerable<Passage> found)
    {
        var message = new StringBuilder("Documentation excerpts:\n\n");
        var index = 1;

        foreach (var passage in found)
        {
            message
                .AppendLine($"[{index}] {passage.Page.Value} — {passage.HeadingPath}")
                .AppendLine(passage.Content)
                .AppendLine();
            index++;
        }

        message.AppendLine($"Question: {question.Value}");

        return message.ToString();
    }
}
