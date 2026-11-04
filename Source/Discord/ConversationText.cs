// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Discord;

/// <summary>Removes transport identities and obvious secret-like text before model input.</summary>
public static partial class ConversationText
{
    [GeneratedRegex(@"<[@#][!&]?\d+>|@[\w.-]+|\b\d{17,20}\b", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Identities { get; }

    /// <summary>Strips Discord mentions and obvious tokens; this is not a guarantee of anonymity.</summary>
    /// <param name="text">Plain message text.</param>
    /// <returns>Sanitized text without author metadata, replacing the entire input if matching times out.</returns>
    public static string Sanitize(string text) => Sanitize(text, SanitizeRecognizedShapes);

    /// <summary>Runs sanitization with a fail-closed timeout boundary.</summary>
    /// <param name="text">The untrusted text.</param>
    /// <param name="sanitize">The sanitization operation.</param>
    /// <returns>Sanitized text, or the removal marker if matching times out.</returns>
    internal static string Sanitize(string text, Func<string, string> sanitize)
    {
        try
        {
            return sanitize(text);
        }
        catch (RegexMatchTimeoutException)
        {
            return SecretRedaction.Marker;
        }
    }

    static string SanitizeRecognizedShapes(string text) => Identities.Replace(SecretRedaction.Redact(text).Text, string.Empty).Trim();
}
