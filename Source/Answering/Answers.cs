// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
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
        var found = (await passages.Search(new(question.Value), answering.MaxPassages, cancellationToken)).ToArray();
        var qualified = found.Where(passage => double.IsFinite(passage.SemanticSimilarity) &&
            passage.SemanticSimilarity is >= -1 and <= 1 && passage.SemanticSimilarity >= answering.MinSemanticSimilarity).ToArray();
        var confidence = qualified.Length > 0 ? qualified[0].SemanticSimilarity : found
            .Where(passage => double.IsFinite(passage.SemanticSimilarity) && passage.SemanticSimilarity is >= -1 and <= 1)
            .Select(passage => passage.SemanticSimilarity).DefaultIfEmpty(0).Max();

        Answer answer;
        if (qualified.Length == 0)
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
                    new(ChatRole.User, ComposeUserMessage(question, qualified))
                ],
                new() { MaxOutputTokens = answering.MaxOutputTokens, ResponseFormat = AnswerGeneration.Format },
                cancellationToken);

            var citations = qualified.Select(passage => passage.Page).Distinct().Take(4).ToArray();
            var generated = AnswerGeneration.Parse(response.Text);
            answer = generated is null
                ? Answer.Refusal(confidence, found)
                : new(generated.Text, citations, confidence, IsRefusal: false, qualified) { Category = generated.Category, Product = generated.Product, CitationProvenance = Provenance(citations, qualified) };
        }

        var interactionId = await interactionLog.Record(
            new(source, answer.Citations, answer.Confidence, answer.IsRefusal) { Category = answer.Category, Product = answer.Product },
            cancellationToken);

        return answer with { InteractionId = interactionId, ContextDegraded = question.Conversation.IsDegraded };
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

        if (question.Conversation.Messages.Count > 0)
        {
            message.AppendLine("<untrusted_conversation_history>")
                .AppendLine(JsonSerializer.Serialize(question.Conversation.Messages))
                .AppendLine("</untrusted_conversation_history>")
                .AppendLine();
        }

        message.AppendLine($"Question: {question.Value}");

        return message.ToString();
    }

    static CitationProvenance[] Provenance(IEnumerable<PageUrl> citations, IEnumerable<Passage> passages) =>
        [.. citations.Select(page => Consistent(page, passages.Where(passage => passage.Page == page).ToArray()))];

    static CitationProvenance Consistent(PageUrl page, Passage[] passages) => new(
        page,
        Agreed(passages, passage => passage.IndexedAt),
        Agreed(passages, passage => passage.PageLastModifiedAt),
        Agreed(passages, passage => passage.ProductVersion));

    /// <summary>Takes a value only when every passage of the page agrees; disputed metadata is omitted rather than guessed.</summary>
    /// <typeparam name="T">The metadata value type.</typeparam>
    /// <param name="passages">The passages of one page.</param>
    /// <param name="select">Selects the metadata value.</param>
    /// <returns>The agreed value, or the default when absent or disputed.</returns>
    static T? Agreed<T>(Passage[] passages, Func<Passage, T?> select) =>
        passages.Select(select).Distinct().ToArray() is [var only] ? only : default;
}
