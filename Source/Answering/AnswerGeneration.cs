// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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
    const string ShortenedMarker = "…";

    static readonly Dictionary<string, InteractionProduct> _products = new(StringComparer.Ordinal)
    {
        ["Chronicle"] = InteractionProduct.Chronicle,
        ["Arc"] = InteractionProduct.Arc,
        ["Fundamentals"] = InteractionProduct.Fundamentals,
        ["Components"] = InteractionProduct.Components,
        ["CLI"] = InteractionProduct.Cli,
        ["Specifications"] = InteractionProduct.Specifications,
        ["Stage"] = InteractionProduct.Stage,
        ["Screenplay"] = InteractionProduct.Screenplay,
        ["Prologue"] = InteractionProduct.Prologue,
        ["Studio"] = InteractionProduct.Studio,
        ["Direct"] = InteractionProduct.Direct,
        ["Assurance"] = InteractionProduct.Assurance
    };

    static readonly JsonElement _schema = JsonDocument.Parse(
        """
        {
          "type": "object",
          "properties": {
            "text": { "type": "string" },
            "category": { "type": "string", "enum": ["answered", "docs-gap", "missing-capability", "likely-bug", "out-of-scope", "unknown"] },
            "product": { "type": "string", "enum": ["Chronicle", "Arc", "Fundamentals", "Components", "CLI", "Specifications", "Stage", "Screenplay", "Prologue", "Studio", "Direct", "Assurance", "other/unknown"] },
            "suggestions": { "type": "array", "items": { "type": "string" } }
          },
          "required": ["text", "category", "product", "suggestions"],
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
                ? new GeneratedAnswer(text, Category(Text(root, "category")), Product(Text(root, "product"))) { Suggestions = Suggestions(root) }
                : null;
        }
        catch (JsonException)
        {
            return CompleteTextOfTruncatedEnvelope(trimmed);
        }
    }

    /// <summary>
    /// A response that hit the output cap is cut mid-envelope. The schema lists <c language="text">text</c> first, so
    /// when that string is fully closed it is the model's complete answer and is kept with Unknown signals.
    /// When the cut falls inside the text, its complete lines are kept and marked as shortened, so a long list
    /// still answers; a cut before the first line break stays unrecoverable and the caller offers pages instead.
    /// </summary>
    /// <param name="envelope">The trimmed response that failed to parse.</param>
    /// <returns>The salvaged answer, or <see langword="null"/> when no complete text or line is recoverable.</returns>
    static GeneratedAnswer? CompleteTextOfTruncatedEnvelope(string envelope)
    {
        var bytes = Encoding.UTF8.GetBytes(envelope);
        var reader = new Utf8JsonReader(bytes, isFinalBlock: false, default);
        try
        {
            var text = ReadFirstTextProperty(ref reader, out var textStart) ?? CompleteLines(bytes, textStart);
            return string.IsNullOrWhiteSpace(text) ? null : new(text, InteractionCategory.Unknown, InteractionProduct.OtherOrUnknown);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Decodes the complete lines of a text string cut by the output cap and marks the answer as shortened.</summary>
    /// <param name="envelope">The UTF-8 envelope.</param>
    /// <param name="start">The offset just after the property name of the text, or -1 when there is none.</param>
    /// <returns>The complete lines with a trailing ellipsis line, or <see langword="null"/> when no line is complete.</returns>
    static string? CompleteLines(byte[] envelope, int start)
    {
        var quote = start < 0 ? -1 : Array.FindIndex(envelope, start, value => value is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or (byte)':'));
        if (quote < 0 || envelope[quote] != (byte)'"')
        {
            return null;
        }

        var lastLineBreak = -1;
        for (var index = quote + 1; index < envelope.Length; index++)
        {
            if (envelope[index] == (byte)'"')
            {
                return null;
            }

            if (envelope[index] == (byte)'\\')
            {
                lastLineBreak = index + 1 < envelope.Length && envelope[index + 1] == (byte)'n' ? index : lastLineBreak;
                index++;
            }
        }

        if (lastLineBreak < 0)
        {
            return null;
        }

        var lines = JsonSerializer.Deserialize<string>((byte[])[.. envelope.AsSpan(quote, lastLineBreak - quote), (byte)'"'])?.TrimEnd();
        return string.IsNullOrWhiteSpace(lines) ? null : $"{lines}\n{ShortenedMarker}";
    }

    static bool Advance(ref Utf8JsonReader reader, JsonTokenType expected) => reader.Read() && reader.TokenType == expected;

    static string? ReadFirstTextProperty(ref Utf8JsonReader reader, out int textStart)
    {
        textStart = -1;
        if (!Advance(ref reader, JsonTokenType.StartObject) || !Advance(ref reader, JsonTokenType.PropertyName))
        {
            return null;
        }

        if (!reader.ValueTextEquals("text"))
        {
            return null;
        }

        textStart = (int)reader.BytesConsumed;
        return Advance(ref reader, JsonTokenType.String) ? reader.GetString() : null;
    }

    static string[] Suggestions(JsonElement root) =>
        root.TryGetProperty("suggestions", out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String).Select(item => item.GetString()!)]
            : [];

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

    static InteractionProduct Product(string? wire) => _products.GetValueOrDefault(Bounded(wire) ?? string.Empty, InteractionProduct.OtherOrUnknown);

    static string? Bounded(string? wire) => wire is { Length: <= MaximumSignalLength } ? wire : null;
}
