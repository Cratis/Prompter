// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Protection;

/// <summary>Removes recognizable secret shapes before any downstream processing.</summary>
public static partial class SecretRedaction
{
    /// <summary>The visible replacement; never includes the removed value.</summary>
    public const string Marker = "[REDACTED_SECRET]";
    const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
    static readonly HashSet<string> _typeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "number", "boolean", "bool", "int", "str", "any", "object", "null", "undefined", "true", "false", "CancellationToken"
    };

    [GeneratedRegex(@"\bBearer\s+[\w\-.~+/]{8,}=*|\bsk-(ant-)?[\w-]{16,}|\bgh[pousr]_\w{16,}|\bgithub_pat_\w{16,}|\b[MN][\w-]{23,25}\.[\w-]{6}\.[\w-]{27,38}\b|\bpa-[\w-]{20,}|\beyJ[\w-]+\.[\w-]+\.[\w-]+|-----BEGIN [A-Z ]*PRIVATE KEY-----[\s\S]*?(-----END [A-Z ]*PRIVATE KEY-----|$)|\bAKIA[0-9A-Z]{16}\b", PatternOptions, 100)]
    private static partial Regex Tokens { get; }

    [GeneratedRegex(@"\b(?<key>api[_-]?key|access[_-]?key|token|pass" + @"word|passwd|pwd|secret|client[_-]?secret|Account" + @"Key|SharedAccess" + @"Key)(?<separator>\s*[:=]\s*)(?<value>""[^""]{4,}""|'[^']{4,}'|[^\s;,'""]{4,})", PatternOptions, 100)]
    private static partial Regex Assignments { get; }

    [GeneratedRegex(@"(?<=:" + @"//)[^\s/:@]+:" + @"[^\s/@]+(?=@)", PatternOptions, 100)]
    private static partial Regex UserInfo { get; }

    /// <summary>Replaces secret-shaped values, preserving harmless type declarations.</summary>
    /// <param name="text">The untrusted text.</param>
    /// <returns>The redacted text and removal flag.</returns>
    public static RedactedText Redact(string text)
    {
        var result = Tokens.Replace(text, Marker);
        result = Assignments.Replace(result, match =>
            _typeWords.Contains(match.Groups["value"].Value.Trim('"', '\''))
                ? match.Value
                : match.Groups["key"].Value + match.Groups["separator"].Value + Marker);
        result = UserInfo.Replace(result, Marker);
        return new(result, !string.Equals(result, text, StringComparison.Ordinal));
    }

    /// <summary>Determines whether the text contains a recognizable secret.</summary>
    /// <param name="text">The untrusted text.</param>
    /// <returns>Whether redaction removes a value.</returns>
    public static bool ContainsSecret(string text) => Redact(text).Removed;
}
