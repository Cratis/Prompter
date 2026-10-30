// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Prompter.GitHub.Authentication;

namespace Cratis.Prompter.GitHub;

/// <summary>Validates bounded metadata without retaining titles, bodies, identities, or remote URLs.</summary>
/// <param name="destination">The installation-approved repository.</param>
/// <param name="closed">Whether this is the closed search bucket.</param>
/// <param name="since">The inclusive lower bound for closed timestamps.</param>
internal sealed class RelatedSearchResponse(GitHubDestination destination, bool closed, DateTimeOffset since)
{
    /// <summary>Constructs fixed qualifiers around plain keywords.</summary>
    /// <param name="keywords">Already sanitized keywords, never raw user text.</param>
    /// <returns>The search expression.</returns>
    public string Query(string keywords)
    {
        var state = closed ? $"is:closed closed:>={since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" : "is:open";
        return $"repo:{destination.Owner}/{destination.Repository} is:issue {state} {keywords}";
    }

    /// <summary>Reads at most three validated hints from a complete search bucket.</summary>
    /// <param name="root">The untrusted response.</param>
    /// <returns>Canonical hints, or no hints for incomplete/malformed results.</returns>
    public IReadOnlyList<RelatedIssue> Read(JsonElement root)
    {
        if (!Complete(root)) return [];
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) return [];
        var result = new List<RelatedIssue>();
        foreach (var item in items.EnumerateArray().Take(3))
        {
            if (Valid(item, out var number))
            {
                var url = $"https://github.com/{destination.Owner}/{destination.Repository}/issues/{number.ToString(CultureInfo.InvariantCulture)}";
                result.Add(new(number, url, closed));
            }
        }
        return result;
    }

    static bool Complete(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return false;
        return root.TryGetProperty("incomplete_results", out var incomplete) && incomplete.ValueKind == JsonValueKind.False;
    }

    static bool Number(JsonElement item, out int number)
    {
        number = 0;
        if (item.ValueKind != JsonValueKind.Object) return false;
        if (!item.TryGetProperty("number", out var id) || id.ValueKind != JsonValueKind.Number) return false;
        return id.TryGetInt32(out number) && number > 0;
    }

    static string? Text(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    bool Valid(JsonElement item, out int number)
    {
        if (!Number(item, out number)) return false;
        if (item.TryGetProperty("pull_request", out _)) return false;
        if (!MatchesRepository(item)) return false;
        return Text(item, "state") == (closed ? "closed" : "open") && InWindow(item);
    }

    bool MatchesRepository(JsonElement item) => string.Equals(Text(item, "repository_url"), $"https://api.github.com/repos/{destination.Owner}/{destination.Repository}", StringComparison.OrdinalIgnoreCase);

    bool InWindow(JsonElement item)
    {
        if (!closed) return true;
        if (!item.TryGetProperty("closed_at", out var date) || date.ValueKind != JsonValueKind.String) return false;
        if (!date.TryGetDateTimeOffset(out var timestamp)) return false;
        return timestamp >= since && timestamp <= since.AddDays(90);
    }
}
