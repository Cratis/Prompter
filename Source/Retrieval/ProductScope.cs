// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Retrieval;

/// <summary>Maps Cratis products to the page URL prefixes that belong to them, and back.</summary>
public static class ProductScope
{
    const string GitHubReleases = "https://github.com/Cratis/";

    static readonly (InteractionProduct Product, string Key, string? Repository, string[] ExtraPrefixes)[] _products =
    [
        (InteractionProduct.Chronicle, "chronicle", "Chronicle", []),
        (InteractionProduct.Arc, "arc", "Arc", []),
        (InteractionProduct.Fundamentals, "fundamentals", "Fundamentals", []),
        (InteractionProduct.Components, "components", "Components", []),
        (InteractionProduct.Cli, "cli", "cli", []),
        (InteractionProduct.Specifications, "specifications", "Specifications", []),
        (InteractionProduct.Stage, "stage", "Stage", []),
        (InteractionProduct.Screenplay, "screenplay", "Screenplay", []),
        (InteractionProduct.Prologue, "prologue", "Prologue", []),
        (InteractionProduct.Studio, "studio", null, ["https://cratis.studio/", "https://cratis.no/studio/"]),
        (InteractionProduct.Direct, "direct", null, ["https://cratis.direct/"]),
        (InteractionProduct.Assurance, "assurance", null, ["https://cratis.no/support/"])
    ];

    /// <summary>Gets the products a question can be scoped to, in display order.</summary>
    public static IReadOnlyList<InteractionProduct> Products { get; } = [.. _products.Select(entry => entry.Product)];

    /// <summary>Gets the page URL prefixes of a product's documentation and release notes.</summary>
    /// <param name="product">The product.</param>
    /// <param name="docsSiteUrl">The base URL of the documentation site.</param>
    /// <returns>The prefixes; empty for <see cref="InteractionProduct.OtherOrUnknown"/>.</returns>
    public static IReadOnlyList<string> PrefixesFor(InteractionProduct product, string docsSiteUrl)
    {
        var entry = _products.FirstOrDefault(candidate => candidate.Product == product);
        if (entry.Key is null) return [];
        var prefixes = DocumentationPrefixes(entry.Key, docsSiteUrl).ToList();
        if (entry.Repository is not null)
        {
            prefixes.Add($"{GitHubReleases}{entry.Repository}/releases/");
            prefixes.Add($"{GitHubReleases}{entry.Repository}/blob/");
        }
        prefixes.AddRange(entry.ExtraPrefixes);
        return prefixes;
    }

    /// <summary>Finds the product a page belongs to.</summary>
    /// <param name="pageUrl">The page URL.</param>
    /// <param name="docsSiteUrl">The base URL of the documentation site.</param>
    /// <returns>The product, or <see langword="null"/> for pages (such as samples) that belong to none.</returns>
    public static InteractionProduct? ProductOf(string pageUrl, string docsSiteUrl)
    {
        foreach (var (product, _, _, _) in _products)
        {
            if (PrefixesFor(product, docsSiteUrl).Any(prefix => prefix.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? pageUrl.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                : pageUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return product;
            }
        }

        return null;
    }

    /// <summary>Gets the stable key of a product, as used in custom ids and documentation paths.</summary>
    /// <param name="product">The product.</param>
    /// <returns>The key, or an empty string for <see cref="InteractionProduct.OtherOrUnknown"/>.</returns>
    public static string Key(InteractionProduct product) =>
        _products.FirstOrDefault(candidate => candidate.Product == product).Key ?? string.Empty;

    /// <summary>Gets the name shown to people.</summary>
    /// <param name="product">The product.</param>
    /// <returns>"CLI" for the CLI, otherwise the product name.</returns>
    public static string Display(InteractionProduct product) => product == InteractionProduct.Cli ? "CLI" : product.ToString();

    /// <summary>Reads a product key.</summary>
    /// <param name="key">The key, such as <c language="text">chronicle</c>.</param>
    /// <param name="product">The product when the key is known.</param>
    /// <returns>Whether the key names a product.</returns>
    public static bool TryParseKey(string key, out InteractionProduct product)
    {
        foreach (var entry in _products)
        {
            if (entry.Key == key)
            {
                product = entry.Product;
                return true;
            }
        }

        product = InteractionProduct.OtherOrUnknown;
        return false;
    }

    /// <summary>Builds the documentation URL prefixes for a path, for both the www and bare host forms.</summary>
    /// <param name="key">The path below the documentation site root.</param>
    /// <param name="docsSiteUrl">The documentation site URL.</param>
    /// <returns>The folder and markdown prefixes for the path.</returns>
    internal static IEnumerable<string> DocumentationPrefixes(string key, string docsSiteUrl)
    {
        var site = docsSiteUrl.TrimEnd('/');
        var roots = new List<string> { site };
        if (Uri.TryCreate(site, UriKind.Absolute, out var uri))
        {
            var twin = new UriBuilder(uri) { Host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : $"www.{uri.Host}" };
            roots.Add(twin.Uri.AbsoluteUri.TrimEnd('/'));
        }

        return roots.SelectMany(root => new[] { $"{root}/{key}/", $"{root}/{key}.md" });
    }
}
