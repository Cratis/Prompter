// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents the bounded, explicit provenance of one documentation page. A value is carried only when the
/// page states it unambiguously; nothing is inferred from prose, code, publish dates or arbitrary numbers.
/// </summary>
/// <param name="Page">The page the metadata describes.</param>
/// <param name="PageLastModified">The explicit ISO <c language="yaml">lastUpdated</c> frontmatter value, when present and valid.</param>
/// <param name="ProductVersion">The explicit semantic version of the page, without a leading <c language="text">v</c>, when stated unambiguously.</param>
public partial record DocumentationMetadata(PageUrl Page, DateTimeOffset? PageLastModified, string? ProductVersion)
{
    const int MaximumFrontmatterLength = 4096;
    const int MaximumVersionLength = 64;

    [GeneratedRegex(@"^(?<key>[A-Za-z][A-Za-z0-9]*):[ \t]*(?<value>.*?)[ \t]*$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Scalar { get; }

    [GeneratedRegex(@"^v?(?<version>\d{1,4}\.\d{1,4}\.\d{1,4}(?:-[0-9A-Za-z][0-9A-Za-z.\-]{0,30})?)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Semver { get; }

    [GeneratedRegex(@"^#[ \t]+(?:Cratis[ \t]+)?(?:Chronicle|Arc|Fundamentals|Components|CLI)[ \t]+(?:v|version[ \t]+v?)(?<version>\d{1,4}\.\d{1,4}\.\d{1,4}(?:-[0-9A-Za-z][0-9A-Za-z.\-]{0,30})?)[ \t]*$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Heading { get; }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}(?::\d{2}(?:\.\d{1,7})?)?(?:Z|[+-]\d{2}:\d{2})?)?$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IsoDate { get; }

    /// <summary>Gets the metadata for a page that states nothing.</summary>
    /// <param name="page">The page.</param>
    /// <returns>Metadata with every optional value absent.</returns>
    public static DocumentationMetadata Empty(PageUrl page) => new(page, null, null);

    /// <summary>Reads the restricted frontmatter scalars and an unambiguous product heading.</summary>
    /// <param name="page">The page URL.</param>
    /// <param name="markdown">The raw page markdown, including frontmatter.</param>
    /// <returns>The page's explicit metadata.</returns>
    public static DocumentationMetadata From(PageUrl page, string markdown)
    {
        var (frontmatter, body) = Split(markdown);
        var modified = Unique(frontmatter, "lastUpdated") is { } date ? ParseDate(date) : null;
        var fromFrontmatter = Version(Unique(frontmatter, "productVersion"), Unique(frontmatter, "version"));
        var fromHeading = HeadingVersion(body);
        var version = (fromFrontmatter, fromHeading) switch
        {
            (null, var heading) => heading,
            (var explicitVersion, null) => explicitVersion,
            var (left, right) => left == right ? left : null
        };
        return new(page, modified, version);
    }

    static (List<string> Lines, string Body) Split(string markdown)
    {
        if (!markdown.StartsWith("---", StringComparison.Ordinal))
        {
            return ([], markdown);
        }

        var end = markdown.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0 || end > MaximumFrontmatterLength)
        {
            return ([], markdown);
        }

        var lines = markdown[3..end].Split('\n').Select(line => line.TrimEnd('\r')).ToList();
        return (lines, markdown[(end + 4)..]);
    }

    static string? Unique(List<string> lines, string key)
    {
        var values = lines
            .Select(line => Scalar.Match(line))
            .Where(match => match.Success && string.Equals(match.Groups["key"].Value, key, StringComparison.Ordinal))
            .Select(match => Unquote(match.Groups["value"].Value))
            .ToArray();
        return values.Length == 1 ? values[0] : null;
    }

    static string Unquote(string value) =>
        value.Length >= 2 && value[0] == value[^1] && value[0] is '"' or '\'' ? value[1..^1] : value;

    static DateTimeOffset? ParseDate(string value) =>
        IsoDate.IsMatch(value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : null;

    static string? Version(string? product, string? generic)
    {
        var candidates = new[] { product, generic }.Where(value => value is not null).Select(value => Parse(value!)).ToArray();
        return candidates.Length > 0 && candidates.All(value => value is not null) && candidates.Distinct().Count() == 1 ? candidates[0] : null;
    }

    static string? Parse(string value) =>
        value.Length <= MaximumVersionLength && Semver.Match(value) is { Success: true } match ? match.Groups["version"].Value : null;

    static string? HeadingVersion(string body)
    {
        var inFence = false;
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("```", StringComparison.Ordinal) || line.StartsWith("~~~", StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (!inFence && line.StartsWith("# ", StringComparison.Ordinal))
            {
                return Heading.Match(line) is { Success: true } match ? match.Groups["version"].Value : null;
            }
        }

        return null;
    }
}
