// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Routing;

/// <summary>Current-first, bounded untrusted text without transport metadata.</summary>
internal static class ClassifierExcerpt
{
    const int MaximumLength = 4000;

    /// <summary>Builds at most 4000 characters without increasing the caller's history bounds.</summary>
    /// <param name="current">Sanitized current request.</param>
    /// <param name="olderRequests">Sanitized, bounded same-public-scope requests.</param>
    /// <returns>The classifier excerpt.</returns>
    public static string Build(string current, IReadOnlyList<string> olderRequests)
    {
        var text = new StringBuilder();
        Append(text, "Untrusted current request:\n");
        Append(text, current);
        foreach (var request in olderRequests.Reverse())
        {
            if (text.Length == MaximumLength) break;
            Append(text, "\nUntrusted older request excerpt:\n");
            Append(text, request);
        }
        return text.ToString();
    }

    static void Append(StringBuilder target, string text) => target.Append(text.AsSpan(0, Math.Min(text.Length, MaximumLength - target.Length)));
}
