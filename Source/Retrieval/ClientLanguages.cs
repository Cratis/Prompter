// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Decides which secondary client-language variant pages (Elixir, Kotlin/Java, Go, the Chronicle TypeScript client)
/// to keep out of the candidate pool, so they do not crowd out the default .NET/C# page when the question
/// names no such language.
/// </summary>
public static partial class ClientLanguages
{
    static readonly (ClientLanguage Language, string Path, bool DefaultEligible)[] _variants =
    [
        (ClientLanguage.TypeScript, "chronicle/clients/typescript", false),
        (ClientLanguage.TypeScript, "arc/backend/typescript", true),
        (ClientLanguage.Jvm, "chronicle/clients/kotlin", false),
        (ClientLanguage.Jvm, "arc/backend/kotlin", false),
        (ClientLanguage.Jvm, "eventmodelers-ai/kotlin", false),
        (ClientLanguage.Jvm, "eventmodelers-ai/java", false),
        (ClientLanguage.Elixir, "chronicle/clients/elixir", false),
        (ClientLanguage.Elixir, "arc/backend/elixir", false),
        (ClientLanguage.Go, "chronicle/go", false),
        (ClientLanguage.Go, "arc/go", false),
        (ClientLanguage.Go, "fundamentals/go", false),
    ];

    static readonly ClientLanguage[] _languages = [ClientLanguage.DotNet, ClientLanguage.TypeScript, ClientLanguage.Jvm, ClientLanguage.Elixir, ClientLanguage.Go];

    [GeneratedRegex(@"(\bc#|\bcsharp\b|\.net\b|\bdotnet\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex DotNet { get; }

    [GeneratedRegex(@"\b(typescript|javascript|node(\.js)?|npm)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex TypeScript { get; }

    [GeneratedRegex(@"\b(kotlin|java|jvm|spring|gradle|maven)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex Jvm { get; }

    [GeneratedRegex(@"\b(elixir|phoenix|erlang)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex Elixir { get; }

    [GeneratedRegex(@"(\bgolang\b|\bgo\s+(client|sdk|module|package|app|service|code|language|lang|version)\b|\b(in|using|via)\s+go\b(?!-)|\b(from|with|for|on)\s+go\s*([?.,!;:)]|$)|\b(chronicle|arc|fundamentals)\.go\b)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 100)]
    private static partial Regex Go { get; }

    /// <summary>Detects the client languages named by a question and the user messages before it.</summary>
    /// <param name="question">The question; its conversation is read here only, and never sent on to retrieval.</param>
    /// <returns>The named languages, or <see cref="ClientLanguage.None"/>.</returns>
    public static ClientLanguage Named(Question question)
    {
        var text = string.Join('\n', question.Conversation.Messages
            .Where(message => message.Role == "user").Select(message => message.Content).Append(question.Value));
        return _languages.Where(language => IsNamed(language, text)).Aggregate(ClientLanguage.None, (all, language) => all | language);
    }

    /// <summary>Gets the documentation prefixes whose pages should be left out of retrieval.</summary>
    /// <param name="named">The client languages the asker named.</param>
    /// <param name="docsSiteUrl">The documentation site URL.</param>
    /// <returns>The excluded prefixes; empty when nothing should be excluded.</returns>
    public static IReadOnlyList<string> ExcludedPrefixes(ClientLanguage named, string docsSiteUrl)
    {
        return [.. _variants
            .Where(variant => !named.HasFlag(variant.Language) && (named != ClientLanguage.None || !variant.DefaultEligible))
            .SelectMany(variant => ProductScope.DocumentationPrefixes(variant.Path, docsSiteUrl))];
    }

    static bool IsNamed(ClientLanguage language, string text)
    {
        var pattern = language switch
        {
            ClientLanguage.DotNet => DotNet,
            ClientLanguage.TypeScript => TypeScript,
            ClientLanguage.Jvm => Jvm,
            ClientLanguage.Elixir => Elixir,
            _ => Go
        };

        try
        {
            return pattern.IsMatch(text);
        }
        catch (RegexMatchTimeoutException)
        {
            // Fail open: an unreadable question excludes nothing.
            return true;
        }
    }
}
