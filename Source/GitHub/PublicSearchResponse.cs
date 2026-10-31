// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Prompter.GitHub;

/// <summary>Strict validation of an anonymous public search response; issue text is untrusted and never read.</summary>
internal static class PublicSearchResponse
{
    /// <summary>The fixed owner of every searchable repository.</summary>
    internal const string Owner = "Cratis";

    /// <summary>The fixed public repositories.</summary>
    internal static readonly IReadOnlyList<string> Repositories = ["Chronicle", "Arc", "Fundamentals", "Components", "CLI"];

    /// <summary>Reads validated issues from a complete response.</summary>
    /// <param name="root">The parsed response.</param>
    /// <param name="closed">Whether this is the closed bucket.</param>
    /// <param name="since">The start of the closed window.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The valid issues, or <see langword="null"/> when the response is malformed or incomplete.</returns>
    internal static List<RelatedIssue>? Read(JsonElement root, bool closed, DateTimeOffset since, DateTimeOffset now)
    {
        return Items(root) is not { } items
            ? null
            : [.. items.EnumerateArray().Take(6).Select(item => Validate(item, closed, since, now)).OfType<RelatedIssue>()];
    }

    static JsonElement? Items(JsonElement root) =>
        IsComplete(root) && root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array ? items : null;

    static bool IsComplete(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object && root.TryGetProperty("incomplete_results", out var incomplete) && incomplete.ValueKind == JsonValueKind.False;

    static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    static string? KnownRepository(JsonElement item) =>
        Repositories.FirstOrDefault(repository => string.Equals(Text(item, "repository_url"), $"https://api.github.com/repos/{Owner}/{repository}", StringComparison.Ordinal));

    static bool IsIssue(JsonElement item) => item.ValueKind == JsonValueKind.Object && !item.TryGetProperty("pull_request", out _);

    static int Number(JsonElement item) =>
        item.TryGetProperty("number", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var number) && number > 0 ? number : 0;

    static bool InWindow(JsonElement item, DateTimeOffset since, DateTimeOffset now) =>
        item.TryGetProperty("closed_at", out var date) && date.ValueKind == JsonValueKind.String &&
        date.TryGetDateTimeOffset(out var closedAt) && closedAt >= since && closedAt <= now.AddMinutes(5);

    static bool StateMatches(JsonElement item, bool closed, DateTimeOffset since, DateTimeOffset now) =>
        Text(item, "state") == (closed ? "closed" : "open") && (!closed || InWindow(item, since, now));

    static RelatedIssue? Validate(JsonElement item, bool closed, DateTimeOffset since, DateTimeOffset now)
    {
        var number = IsIssue(item) ? Number(item) : 0;
        var repository = number == 0 ? null : KnownRepository(item);
        return repository is null || !StateMatches(item, closed, since, now)
            ? null
            : new RelatedIssue(number, $"https://github.com/{Owner}/{repository}/issues/{number.ToString(CultureInfo.InvariantCulture)}", closed) { Repository = repository };
    }
}
