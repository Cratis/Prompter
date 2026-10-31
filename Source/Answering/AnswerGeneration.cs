// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Owns the structured answer schema and the bounded, non-throwing parsing of a model response into a
/// <see cref="GeneratedAnswer"/>. A plain-text response stays usable; classification never blocks the text.
/// </summary>
public static class AnswerGeneration
{
    const int MaximumSignalLength = 32;

    static readonly JsonElement _schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "text": { "type": "string" },
            "category": { "type": "string", "enum": ["answered", "docs-gap", "missing-capability", "likely-bug", "out-of-scope", "unknown"] },
            "product": { "type": "string", "enum": ["Chronicle", "Arc", "Fundamentals", "Components", "CLI", "other/unknown"] }
          },
          "required": ["text", "category", "product"],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    /// <summary>Gets the response format that requests the structured envelope.</summary>
    public static ChatResponseFormat Format { get; } = ChatResponseFormat.ForJsonSchema(_schema, "prompter_answer", "A grounded answer with triage signals.");

    /// <summary>Parses a response without throwing and without logging its content.</summary>
    /// <param name="response">The raw response text.</param>
    /// <returns>The generated answer, or <see langword="null"/> when it is a structured envelope with no recoverable text.</returns>
    public static GeneratedAnswer? Parse(string response)
    {
        var trimmed = response.Trim();
        if (!trimmed.StartsWith('{'))
        {
            return string.IsNullOrWhiteSpace(trimmed) ? null : new(response, InteractionCategory.Unknown, InteractionProduct.OtherOrUnknown);
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object && Text(root, "text") is { } text && !string.IsNullOrWhiteSpace(text)
                ? new GeneratedAnswer(text, Category(Text(root, "category")), Product(Text(root, "product")))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    static InteractionCategory Category(string? wire) => Bounded(wire) switch
    {
        "answered" => InteractionCategory.Answered,
        "docs-gap" => InteractionCategory.DocsGap,
        "missing-capability" => InteractionCategory.MissingCapability,
        "likely-bug" => InteractionCategory.LikelyBug,
        "out-of-scope" => InteractionCategory.OutOfScope,
        _ => InteractionCategory.Unknown
    };

    static InteractionProduct Product(string? wire) => Bounded(wire) switch
    {
        "Chronicle" => InteractionProduct.Chronicle,
        "Arc" => InteractionProduct.Arc,
        "Fundamentals" => InteractionProduct.Fundamentals,
        "Components" => InteractionProduct.Components,
        "CLI" => InteractionProduct.Cli,
        _ => InteractionProduct.OtherOrUnknown
    };

    static string? Bounded(string? wire) => wire is { Length: <= MaximumSignalLength } ? wire : null;
}
