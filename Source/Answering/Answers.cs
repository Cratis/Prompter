// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Anthropic.Models.Messages;
using Cratis.Prompter.Ingestion;
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
public partial class Answers(
    IPassages passages,
    IChatClient chatClient,
    IInteractionLog interactionLog,
    IOptions<PrompterOptions> options,
    ILogger<Answers> logger) : IAnswers
{
    const int MaximumClarifiedQuestionLength = 1500;

    [GeneratedRegex(@"\[(?<index>\d{1,2})\]", RegexOptions.ExplicitCapture, 100)]
    private static partial Regex CitationIndex { get; }

    /// <inheritdoc/>
    public async Task<Answer> For(Question question, string source, CancellationToken cancellationToken = default)
    {
        var answering = options.Value.Answering;
        var candidates = (await passages.Search(new(question.Value) { Scope = question.Scope }, Math.Max(answering.MaxPassages, Passages.MaximumCandidates), cancellationToken))
            .Where(passage => SourceHosts.IsCitable(passage.Page, options.Value)).ToArray();
        var found = candidates.Take(answering.MaxPassages).ToArray();
        var qualified = candidates.Where(passage => double.IsFinite(passage.SemanticSimilarity) &&
            passage.SemanticSimilarity is >= -1 and <= 1 && passage.SemanticSimilarity >= answering.MinSemanticSimilarity).Take(answering.MaxPassages).ToArray();
        var confidence = qualified.Length > 0 ? qualified[0].SemanticSimilarity : candidates
            .Where(passage => double.IsFinite(passage.SemanticSimilarity) && passage.SemanticSimilarity is >= -1 and <= 1)
            .Select(passage => passage.SemanticSimilarity).DefaultIfEmpty(0).Max();

        var mode = AnswerModes.For(question.Value);
        Answer answer;
        if (qualified.Length == 0)
        {
            logger.RefusingToAnswer(confidence);
            answer = Answer.Refusal(confidence, found);
        }
        else if (Clarify(question, qualified, mode) is { } clarification)
        {
            // Nothing is spent beyond the embedding retrieval already used, and nothing is recorded.
            return new Answer(Clarifications.Text(clarification, question.Value), [], confidence, IsRefusal: false, qualified)
            {
                Clarification = clarification,
                ContextDegraded = question.Conversation.IsDegraded,
                Mode = mode
            };
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
                    new(ChatRole.User, ComposeUserMessage(question, qualified, mode))
                ],
                new() { MaxOutputTokens = answering.MaxOutputTokens, ResponseFormat = AnswerGeneration.Format },
                cancellationToken);

            var generated = AnswerGeneration.Parse(response.Text);
            var citations = CitedPages(generated?.Text, qualified);
            answer = generated is null
                ? Answer.Refusal(confidence, found)
                : new(generated.Text, citations, confidence, IsRefusal: false, qualified) { Category = generated.Category, Product = generated.Product, CitationProvenance = Provenance(citations, qualified), Suggestions = Suggest(generated, question) };
        }

        var interactionId = await interactionLog.Record(
            new(source, answer.Citations, answer.Confidence, answer.IsRefusal) { Category = answer.Category, Product = answer.Product },
            cancellationToken);

        return answer with { InteractionId = interactionId, ContextDegraded = question.Conversation.IsDegraded, Mode = mode };
    }

    static string ComposeUserMessage(Question question, IEnumerable<Passage> found, AnswerMode mode)
    {
        var message = new StringBuilder("Documentation excerpts:\n\n<untrusted_excerpts>\n");
        var index = 1;

        foreach (var passage in found)
        {
            message
                .AppendLine($"[{index}] {Evidence(passage.Page.Value)} — {Evidence(passage.HeadingPath)}")
                .AppendLine(Evidence(passage.Content))
                .AppendLine();
            index++;
        }

        message.AppendLine("</untrusted_excerpts>").AppendLine();

        if (question.Conversation.Messages.Count > 0)
        {
            message.AppendLine("<untrusted_conversation_history>")
                .AppendLine(JsonSerializer.Serialize(question.Conversation.Messages))
                .AppendLine("</untrusted_conversation_history>")
                .AppendLine();
        }

        if (mode == AnswerMode.Recommendation)
        {
            message.AppendLine("Answer mode: recommendation");
        }

        message.AppendLine($"Question: {question.Value}");

        return message.ToString();
    }

    static string Evidence(string text) => text.Replace("untrusted_excerpts", "untrusted-excerpts", StringComparison.OrdinalIgnoreCase);

    static PageUrl[] CitedPages(string? text, Passage[] qualified)
    {
        var ranked = qualified.Select(passage => passage.Page).Distinct().ToArray();
        var referenced = CitationIndex.Matches(text ?? string.Empty)
            .Select(match => int.Parse(match.Groups["index"].Value, System.Globalization.CultureInfo.InvariantCulture))
            .Where(index => index >= 1 && index <= qualified.Length)
            .Select(index => qualified[index - 1].Page);
        var selected = referenced.Concat(ranked).Distinct().Take(4).ToHashSet();
        return [.. ranked.Where(selected.Contains)];
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

    IReadOnlyList<string> Suggest(GeneratedAnswer generated, Question question) =>
        options.Value.Answering.SuggestFollowUps ? FollowUps.From(generated.Suggestions, question.Value) : [];

    Clarification? Clarify(Question question, Passage[] qualified, AnswerMode mode) =>
        question.AllowClarification && mode == AnswerMode.Standard && options.Value.Answering.ClarifyAmbiguousProducts && question.Value.Length <= MaximumClarifiedQuestionLength
            ? Clarifications.Detect(question, qualified, options.Value.DocsSiteUrl, options.Value.Answering.ClarificationMargin)
            : null;
}
