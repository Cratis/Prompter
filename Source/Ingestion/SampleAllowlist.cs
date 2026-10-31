// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>Matches curated paths while excluding generated, private and oversized files.</summary>
public static class SampleAllowlist
{
    static readonly string[] _excludedDirectories = ["bin", "obj", "node_modules"];
    static readonly string[] _excludedExtensions = [".g.cs", ".designer.cs", ".env", ".pfx", ".pem", ".key", ".snk"];

    /// <summary>Determines whether a tree entry is safe and explicitly allowlisted.</summary>
    /// <param name="path">The case-sensitive repository path.</param>
    /// <param name="patterns">The curated glob patterns.</param>
    /// <param name="size">The file size in bytes.</param>
    /// <param name="maxBytes">The maximum accepted byte count.</param>
    /// <returns>Whether this file may be fetched.</returns>
    public static bool Allows(string path, IEnumerable<string> patterns, long size, long maxBytes)
    {
        var segments = path.Split('/');
        return size >= 0 && size <= maxBytes && !segments.Any(ExcludedSegment) && !ExcludedFile(segments[^1]) &&
            patterns.Any(pattern => Regex.IsMatch(path, Glob(pattern), RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)));
    }

    static bool ExcludedSegment(string segment) =>
        segment.Length == 0 || segment.StartsWith('.') || _excludedDirectories.Contains(segment, StringComparer.Ordinal) ||
        segment.Contains("generated", StringComparison.OrdinalIgnoreCase);

    static bool ExcludedFile(string name)
    {
        var lower = name.ToLowerInvariant();
        return lower.StartsWith("appsettings", StringComparison.Ordinal) ||
            _excludedExtensions.Any(extension => lower.EndsWith(extension, StringComparison.Ordinal));
    }

    static string Glob(string pattern) => "^" + Regex.Escape(pattern)
        .Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal)
        .Replace(@"\*\*", ".*", StringComparison.Ordinal)
        .Replace(@"\*", "[^/]*", StringComparison.Ordinal) + "$";
}
