// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Anthropic.Models.Messages;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Cratis.Prompter.Telemetry;
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
    const int MaximumClarifiedQuestionLength = 1500;
    const int MaximumPageExpansions = 2;
    const int MaximumSectionExpansionBytes = 8000;
    const int InputReserveBytes = 1024;
    const int FramingReserveBytes = 512;

    /// <inheritdoc/>
    public async Task<Answer> For(Question question, string source, CancellationToken cancellationToken = default)
    {
        var answering = options.Value.Answering;
        var (candidates, qualified) = await Retrieve(question, cancellationToken);
        if (qualified.Length == 0 && question.Scope is not null)
        {
            (candidates, qualified) = await Retrieve(question with { Scope = null }, cancellationToken);
        }

        var found = candidates.Take(answering.MaxPassages).ToArray();
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
            logger.AnsweringWithPassage(qualified[0].Page.Value, qualified[0].SemanticSimilarity);
            var generated = await Generate(question, qualified, mode, cancellationToken) ?? new GeneratedAnswer(
                "I found relevant pages but couldn't compose an answer. These pages may help.",
                InteractionCategory.Unknown,
                InteractionProduct.OtherOrUnknown);
            var (text, citations) = CitedPages(generated.Text, qualified);
            answer = new(text, citations, confidence, IsRefusal: false, qualified) { Category = generated.Category, Product = generated.Product, CitationProvenance = Provenance(citations, qualified), Suggestions = Suggest(generated, question) };
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

        foreach (var page in found.GroupBy(passage => passage.Page))
        {
            message.AppendLine($"[{index}] {Evidence(page.Key.Value)}");
            foreach (var passage in InChunkOrder(page))
            {
                message.AppendLine($"— {Evidence(passage.HeadingPath)}")
                    .AppendLine(Evidence(passage.Content))
                    .AppendLine();
            }
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

    static IEnumerable<Passage> InChunkOrder(IEnumerable<Passage> page) =>
        page.All(passage => passage.ChunkId is not null)
            ? page.OrderBy(passage => ChunkOrder.Of(passage.ChunkId))
            : page;

    static string Evidence(string text) => text.Replace("untrusted_excerpts", "untrusted-excerpts", StringComparison.OrdinalIgnoreCase);

    static (string Text, PageUrl[] Citations) CitedPages(string text, Passage[] qualified)
    {
        var ranked = qualified.Select(passage => passage.Page).Distinct().ToArray();
        var referenced = ProseCitations.Numbers(text)
            .Where(number => number > 0 && number <= ranked.Length).Distinct().Order().ToArray();
        var numbers = referenced.Select((number, index) => (number, index)).ToDictionary(pair => pair.number, pair => pair.index + 1);
        var normalized = ProseCitations.Remap(text, numbers);
        return (normalized, referenced.Length > 0 ? [.. referenced.Select(number => ranked[number - 1])] : [.. ranked.Take(4)]);
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

    async Task<(Passage[] Candidates, Passage[] Qualified)> Retrieve(Question question, CancellationToken cancellationToken)
    {
        var answering = options.Value.Answering;
        var retrieved = (await passages.Search(new(question.Value) { Scope = question.Scope }, Math.Max(answering.MaxPassages, Passages.MaximumCandidates), cancellationToken))
            .Where(passage => SourceHosts.IsCitable(passage.Page, options.Value)).ToArray();
        var candidates = retrieved.Where(passage => !passage.IsSectionExpansion).ToArray();
        var eligible = candidates.Where(passage => passage.ClearsFloor(answering.MinSemanticSimilarity)).ToArray();
        var qualified = eligible.Where(passage => !passage.IsPageExpansion).Take(answering.MaxPassages).ToList();
        var summaryPages = qualified.Where(passage => passage.IsSummary).Select(passage => passage.Page).ToHashSet();
        var expansionCandidates = eligible.Where(passage => passage.IsPageExpansion && !passage.IsSummary && summaryPages.Contains(passage.Page))
            .OrderByDescending(passage => passage.IsCurated)
            .ThenByDescending(passage => passage.SemanticSimilarity)
            .DistinctBy(passage => (passage.Page, passage.HeadingPath, passage.Content))
            .Where(passage => !qualified.Exists(existing => existing.Page == passage.Page && existing.HeadingPath == passage.HeadingPath && existing.Content == passage.Content))
            .ToArray();

        // Curated leads come from the overview intent, one per curated page, so they take priority and may exceed the
        // ordinary two-slot reservation, still within MaxPassages.
        var expansion = expansionCandidates
            .Take(Math.Min(Math.Max(MaximumPageExpansions, expansionCandidates.Count(passage => passage.IsCurated)), answering.MaxPassages - qualified.Count(passage => passage.IsSummary))).ToArray();
        while (qualified.Count + expansion.Length > answering.MaxPassages)
        {
            qualified.RemoveAt(qualified.FindLastIndex(passage => !passage.IsSummary));
        }
        Passage[] grounded = [.. qualified, .. expansion];
        return (candidates, grounded.Length == 0
            ? grounded
            : [.. grounded, .. SectionExpansions.Select(retrieved, grounded, SectionBudget(question, qualified, expansion))]);
    }

    /// <summary>Gets the bytes section expansion may add while the answer input stays under the input ceiling.</summary>
    /// <param name="question">The question.</param>
    /// <param name="qualified">The passages that met the similarity threshold.</param>
    /// <param name="expansion">The page detail passages already added.</param>
    /// <returns>The byte budget.</returns>
    int SectionBudget(Question question, IEnumerable<Passage> qualified, IEnumerable<Passage> expansion)
    {
        var baseline = Encoding.UTF8.GetByteCount(ComposeUserMessage(question, [.. qualified, .. expansion], AnswerModes.For(question.Value)));
        var available = options.Value.Protection.AnthropicAnswer.MaxInputTokens - InputReserveBytes - Encoding.UTF8.GetByteCount(SystemPrompt.Text) - baseline - FramingReserveBytes;
        return Math.Clamp(available, 0, MaximumSectionExpansionBytes);
    }

    async Task<GeneratedAnswer?> Generate(Question question, Passage[] qualified, AnswerMode mode, CancellationToken cancellationToken)
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
            new() { MaxOutputTokens = options.Value.Answering.MaxOutputTokens, ResponseFormat = AnswerGeneration.Format },
            cancellationToken);
        var generated = AnswerGeneration.Parse(response.Text);
        if (response.FinishReason == ChatFinishReason.Length)
        {
            logger.AnswerReachedOutputLimit(options.Value.Answering.MaxOutputTokens, response.Usage?.OutputTokenCount, generated is not null);
            ProviderMetrics.Event("anthropic", "output_limit");
        }

        if (generated is null)
        {
            logger.GenerationParseFailed(response.Text.Length, response.FinishReason?.ToString());
            return generated;
        }

        var stripped = RetrievalMetaTalk.Strip(generated.Text);
        if (stripped != generated.Text)
        {
            logger.RemovedRetrievalMetaTalk();
            ProviderMetrics.Event("anthropic", "meta_talk");
        }

        return generated with { Text = stripped };
    }

    IReadOnlyList<string> Suggest(GeneratedAnswer generated, Question question) =>
        options.Value.Answering.SuggestFollowUps ? FollowUps.From(generated.Suggestions, question.Value) : [];

    Clarification? Clarify(Question question, Passage[] qualified, AnswerMode mode) =>
        question.AllowClarification && mode == AnswerMode.Standard && options.Value.Answering.ClarifyAmbiguousProducts && question.Value.Length <= MaximumClarifiedQuestionLength
            ? Clarifications.Detect(question, [.. qualified.Where(passage => !passage.IsSectionExpansion)], options.Value.DocsSiteUrl, options.Value.Answering.ClarificationMargin)
            : null;
}
