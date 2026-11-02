// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Protection;

/// <summary>
/// Makes unapproved destinations and mentions inert in generated text, independent of any Markdown or Discord parsing.
/// </summary>
/// <remarks>
/// One context-free pass covers the whole text, code blocks included, so the result is safe however a renderer
/// or the message splitter treats fences, spans or blocks. Nothing is classified as code or prose.
/// <list type="bullet">
/// <item>A URL-like token (anything containing <c language="text">://</c>, a <c language="text">scheme:</c> prefix, a <c language="text">www.</c> prefix,
/// or a bare <c language="text">host.tld/path</c>) that is not citable per <see cref="SourceHosts.IsCitable"/> is defanged: every
/// ASCII or Unicode full-stop and every colon in it becomes bracketed (<c language="text">[.]</c>, <c language="text">[:]</c>), so it never forms a link.
/// Citable tokens are kept byte-identical.</item>
/// <item>A masked link <c language="text">[label](destination)</c> with an unapproved destination becomes <c language="text">label (defanged destination)</c>.</item>
/// <item>Every <c language="text">@</c> becomes the visible fullwidth <c language="text">＠</c> (U+FF20), and every <c language="text">&lt;</c> that precedes
/// <c language="text">@</c>, <c language="text">#</c> or <c language="text">/</c> becomes <c language="text">＜</c> (U+FF1C). This breaks <c language="text">@everyone</c>,
/// <c language="text">@here</c>, user, role, channel and command references without hidden characters, so a reviewer still sees the text.</item>
/// </list>
/// When text exceeds <see cref="MaximumLength"/> or a match times out, a deterministic fallback defangs every
/// <c language="text">.</c> and <c language="text">:</c> (so scheme-less invites are inert too), <c language="text">@</c> and <c language="text">&lt;</c>.
/// A citable-looking token that embeds another destination (a second <c language="text">://</c>, <c language="text">www.</c> or an invite host after the host) is treated as not citable. Uncertainty always defangs more, never less.
/// The transformation is idempotent.
/// </remarks>
public static partial class PublishedMarkdown
{
    /// <summary>The longest text the pattern pass accepts; longer text uses the deterministic fallback.</summary>
    public const int MaximumLength = 32768;

    const char FullwidthAt = '\uFF20';
    const char FullwidthLess = '\uFF1C';
    const string Dots = ".\u3002\uFF0E\uFF61\u2024";
    const string Colons = ":\uFF1A";

    static readonly string[] _embeddedMarkers = ["://", "www.", "discord.gg", "discord.com/invite", "discordapp.com/invite"];

    [GeneratedRegex(
        @"(?<link>!?\[(?<label>[^\[\]\r\n]*)\]\((?<destination>(?:[^()\r\n]|\([^()\r\n]*\))*)\))|(?<url>(?:[^\s<>\[\]()""`]*://|[a-z][a-z0-9+.\-]*:|(?<![\w.\-])www\.)[^\s<>\[\]()""`]+|(?<![\w@.\-/])(?:[a-z0-9\-]+\.)+[a-z0-9\-]+/[^\s<>\[\]()""`]*)|(?<at>@)|(?<angle><(?=[@#/]))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        1000)]
    private static partial Regex Tokens { get; }

    /// <summary>
    /// Applies the publication policy before a generated payload is frozen or displayed.
    /// </summary>
    /// <param name="markdown">Generated text, including any code examples.</param>
    /// <param name="options">The existing citation source policy.</param>
    /// <returns>Text in which only citable destinations remain active and mentions cannot fire.</returns>
    public static string Neutralize(string markdown, PrompterOptions options)
    {
        if (markdown.Length > MaximumLength)
        {
            return Fallback(markdown);
        }
        try
        {
            return Transform(markdown, options);
        }
        catch (RegexMatchTimeoutException)
        {
            return Fallback(markdown);
        }
    }

    static string Transform(string text, PrompterOptions options) => Tokens.Replace(text, match => Replace(match, options));

    static string Replace(Match token, PrompterOptions options)
    {
        if (token.Groups["at"].Success) return FullwidthAt.ToString();
        if (token.Groups["angle"].Success) return FullwidthLess.ToString();
        if (token.Groups["link"].Success) return Link(token, options);

        var url = token.Value.TrimEnd('.', ',', ';', ':', '!', '?');
        return (Citable(url, options) ? url : Defang(url)) + token.Value[url.Length..];
    }

    static string Link(Match token, PrompterOptions options)
    {
        var destination = token.Groups["destination"].Value;
        var originalLabel = token.Groups["label"].Value;
        var label = Transform(originalLabel, options);
        if (!Citable(destination, options))
        {
            return $"{label} ({Defang(destination)})";
        }
        return $"{(token.Value.StartsWith('!') ? "!" : string.Empty)}[{label}]({destination})";
    }

    static bool Citable(string destination, PrompterOptions options)
    {
        if (destination.IndexOfAny(['%', '\\', '&', '`', '@']) >= 0 || destination.Any(char.IsWhiteSpace)) return false;
        var explicitHttps = destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var bareHost = !destination.Contains(':', StringComparison.Ordinal) && !destination.StartsWith('/');
        return (explicitHttps || bareHost) && !EmbedsAnotherDestination(destination) && SourceHosts.IsCitable(new(explicitHttps ? destination : "https://" + destination), options);
    }

    static bool EmbedsAnotherDestination(string destination)
    {
        var hostEnd = destination.IndexOf('/', destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? 8 : 0);
        return hostEnd >= 0 && _embeddedMarkers.Any(marker => destination.AsSpan(hostEnd).Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    static string Defang(string text)
    {
        var output = new StringBuilder(text.Length + 8);
        for (var index = 0; index < text.Length; index++)
        {
            output.Append(DefangedCharacter(text, index));
        }
        return output.ToString();
    }

    static string Fallback(string text)
    {
        var output = new StringBuilder(text.Length + 64);
        for (var index = 0; index < text.Length; index++)
        {
            output.Append(FallbackCharacter(text, index));
        }
        return output.ToString();
    }

    static string DefangedCharacter(string text, int index)
    {
        if (Dots.Contains(text[index])) return "[.]";
        return Colons.Contains(text[index]) ? "[:]" : Mention(text, index);
    }

    static string FallbackCharacter(string text, int index)
    {
        if (IsBracketed(text, index)) return text[index].ToString();
        if (Dots.Contains(text[index])) return "[.]";
        if (Colons.Contains(text[index])) return "[:]";
        return text[index] is '<' ? FullwidthLess.ToString() : Mention(text, index);
    }

    static bool IsBracketed(string text, int index) => index > 0 && index + 1 < text.Length && text[index - 1] == '[' && text[index + 1] == ']';

    static string Mention(string text, int index) => text[index] switch
    {
        '@' => FullwidthAt.ToString(),
        '<' when index + 1 < text.Length && text[index + 1] is '@' or '#' or '/' => FullwidthLess.ToString(),
        var other => other.ToString()
    };
}
