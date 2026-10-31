// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Discord;

/// <summary>Removes transport identities and obvious secret-like text before model input.</summary>
public static partial class ConversationText
{
    [GeneratedRegex(@"<[@#][!&]?\d+>|@[\w.-]+|\b\d{17,20}\b", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Identities { get; }

    [GeneratedRegex(@"\bBearer\s+\S+|\b(?:api[_-]?key|token|password|secret)\s*[:=]\s*\S+|\b(?:sk-[\w-]{16,}|gh[pousr]_[\w]{16,}|github_pat_[\w]{16,}|eyJ[\w-]+\.[\w-]+\.[\w-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 100)]
    private static partial Regex Secrets { get; }

    /// <summary>Strips Discord mentions and obvious tokens; this is not a guarantee of anonymity.</summary>
    /// <param name="text">Plain message text.</param>
    /// <returns>Sanitized text without author metadata.</returns>
    public static string Sanitize(string text) => Identities.Replace(Secrets.Replace(text, "[redacted]"), string.Empty).Trim();
}
