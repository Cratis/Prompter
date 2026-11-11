// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reconciles explicit product versions from frontmatter and the first product heading.</summary>
internal static partial class DocumentationVersion
{
    const int MaximumVersionLength = 64;

    [GeneratedRegex(@"^v?(?<version>\d{1,4}\.\d{1,4}\.\d{1,4}(?:-[0-9A-Za-z][0-9A-Za-z.\-]{0,30})?)$", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex Semver { get; }

    [GeneratedRegex(@"^#[ \t]+(?:Cratis[ \t]+)?(?:Chronicle|Arc|Fundamentals|Components|CLI)[ \t]+(?:v|version[ \t]+v?)(?<version>\d{1,4}\.\d{1,4}\.\d{1,4}(?:-[0-9A-Za-z][0-9A-Za-z.\-]{0,30})?)[ \t]*$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Heading { get; }

    /// <summary>Retains a version only when the explicit sources agree.</summary>
    /// <param name="product">The productVersion scalar.</param>
    /// <param name="generic">The version scalar.</param>
    /// <param name="body">The markdown body.</param>
    /// <returns>The unambiguous version, if any.</returns>
    internal static string? Resolve(string? product, string? generic, string body) =>
        (FromFrontmatter(product, generic), FromHeading(body)) switch
        {
            (null, var heading) => heading,
            (var explicitVersion, null) => explicitVersion,
            var (left, right) => left == right ? left : null
        };

    static string? FromFrontmatter(string? product, string? generic)
    {
        var candidates = new[] { product, generic }.Where(value => value is not null).Select(value => Parse(value!)).ToArray();
        return IsUnambiguous(candidates) ? candidates[0] : null;
    }

    static bool IsUnambiguous(string?[] candidates) =>
        candidates.Length > 0 && candidates.All(value => value is not null) && candidates.Distinct().Count() == 1;

    static string? Parse(string value) =>
        value.Length <= MaximumVersionLength && Semver.Match(value) is { Success: true } match ? match.Groups["version"].Value : null;

    static bool IsFence(string line) => line.StartsWith("```", StringComparison.Ordinal) || line.StartsWith("~~~", StringComparison.Ordinal);

    static string? FromHeading(string body)
    {
        var inFence = false;
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (IsFence(line))
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
