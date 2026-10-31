// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Digest;

/// <summary>
/// Composes a bounded, mention-free report from interaction metadata.
/// </summary>
public static class DocsGapReport
{
    const string DocsGap = "docs-gap";
    const string MissingCapability = "missing-capability";
    const string LikelyBug = "likely-bug";

    /// <summary>
    /// Groups gap and thumbs-down counts by sanitized product, without quoting interactions.
    /// </summary>
    /// <param name="rows">The stored interaction metadata.</param>
    /// <param name="from">The inclusive UTC start.</param>
    /// <param name="to">The exclusive UTC end.</param>
    /// <returns>A message of at most 2,000 characters.</returns>
    public static string Compose(IReadOnlyList<DocsGapRow> rows, DateTimeOffset from, DateTimeOffset to)
    {
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"**Prompter docs-gap digest** · {from:yyyy-MM-dd} → {to:yyyy-MM-dd} (UTC)")
        };
        AddSection(lines, "Docs gaps", rows.Where(row => Category(row) == DocsGap));
        AddSection(lines, "Possible missing capabilities", rows.Where(row => Category(row) == MissingCapability));
        AddSection(lines, "Likely bugs", rows.Where(row => Category(row) == LikelyBug));
        AddSection(lines, "Thumbs-down answers", rows.Where(row => row.Feedback == "down"));
        if (lines.Count == 1)
        {
            lines.Add("No docs gaps, possible missing capabilities, likely bugs or thumbs-down answers this week.");
        }

        var older = rows.Count(row => row.WasRefusal && Unclassified(row.Category));
        if (older > 0)
        {
            lines.Add(string.Create(CultureInfo.InvariantCulture, $"_{older} older refusals had no category and are counted as docs gaps._"));
        }

        return string.Join('\n', lines);
    }

    static bool Unclassified(string? category) => category is not (DocsGap or MissingCapability or LikelyBug or "answered" or "out-of-scope");

    static string? Category(DocsGapRow row)
    {
        if (Unclassified(row.Category))
        {
            return row.WasRefusal ? DocsGap : null;
        }

        return row.Category;
    }

    static string Product(DocsGapRow row)
    {
        var product = row.Product;
        if (product is null || product == "unknown")
        {
            var firstPage = row.CitedPages.Count > 0 ? row.CitedPages[0] : null;
            product = PageProduct(firstPage);
            if (product?.EndsWith(".md", StringComparison.OrdinalIgnoreCase) == true)
            {
                product = product[..^3];
            }
        }

        product = product?.ToLowerInvariant();
        return product is { Length: >= 1 and <= 40 } && product.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-') ? product : "unknown";

        static string? PageProduct(string? page)
        {
            if (!Uri.TryCreate(page, UriKind.Absolute, out var url)) return null;
            var segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (!url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return segments.FirstOrDefault();
            var repository = segments.Length > 1 && segments[0].Equals("Cratis", StringComparison.OrdinalIgnoreCase) ? segments[1].ToLowerInvariant() : null;
            return "chronicle|arc|fundamentals|components|cli|samples".Split('|').Contains(repository, StringComparer.Ordinal) ? repository : "unknown";
        }
    }

    static void AddSection(List<string> lines, string title, IEnumerable<DocsGapRow> rows)
    {
        var groups = rows.GroupBy(Product).Select(group => (Product: group.Key, Count: group.Count()))
            .OrderByDescending(group => group.Count).ThenBy(group => group.Product, StringComparer.Ordinal).ToArray();
        if (groups.Length == 0)
        {
            return;
        }

        var products = new List<string>();
        foreach (var group in groups.Take(10))
        {
            var text = string.Create(CultureInfo.InvariantCulture, $"{group.Product} {group.Count}");

            // Leave room for headings, omitted-group counts and the legacy footer within Discord's limit.
            if (string.Join(" · ", products.Append(text)).Length > 350)
            {
                break;
            }

            products.Add(text);
        }

        var omitted = groups.Length - products.Count;
        var suffix = omitted > 0 ? string.Create(CultureInfo.InvariantCulture, $" · …and {omitted} more") : string.Empty;
        lines.Add($"**{title}** — {string.Join(" · ", products)}{suffix}");
    }
}
