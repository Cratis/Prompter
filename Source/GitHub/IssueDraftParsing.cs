// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Reads a drafted issue back out of the model's reply.
/// </summary>
/// <remarks>
/// Tolerant on purpose. The prompt asks for bare JSON, but models wrap it in a code fence often enough that
/// refusing such a reply would fail a report the person already took the trouble to make. Anything that is
/// not recoverable returns <see langword="null"/> so the caller can say "I could not draft that" rather than
/// filing something malformed.
/// </remarks>
public static class IssueDraftParsing
{
    /// <summary>
    /// Parses a drafted issue from the model's reply.
    /// </summary>
    /// <param name="reply">The raw reply text.</param>
    /// <returns>The draft, or <see langword="null"/> when the reply holds no usable JSON object.</returns>
    public static IssueDraft? Parse(string? reply)
    {
        var json = ExtractJson(reply);
        if (json is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var title = ReadString(root, "title");
            var body = ReadString(root, "body");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return new IssueDraft(
                title.Trim(),
                body.Trim(),
                ParseKind(ReadString(root, "kind")),
                ReadString(root, "product")?.Trim() ?? string.Empty);
        }
        catch (JsonException)
        {
            // The reply looked like JSON but was not; the caller reports that it could not be drafted.
            return null;
        }
    }

    /// <summary>
    /// Maps the model's <c>kind</c> value onto <see cref="IssueKind"/>.
    /// </summary>
    /// <param name="kind">The value from the reply.</param>
    /// <returns>
    /// The matching kind. An unrecognized or missing value becomes <see cref="IssueKind.Idea"/>, the kind
    /// that claims least: labelling a real bug an idea is a mislabel, while labelling an idea a bug puts it
    /// in a triage queue that expects a reproduction.
    /// </returns>
    public static IssueKind ParseKind(string? kind) => kind?.Trim().ToLowerInvariant() switch
    {
        "bug" => IssueKind.Bug,
        "feature" => IssueKind.Feature,
        "documentation" or "docs" => IssueKind.Documentation,
        _ => IssueKind.Idea
    };

    static string? ExtractJson(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return null;
        }

        var text = reply.Trim();
        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');

        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }

    static string? ReadString(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
