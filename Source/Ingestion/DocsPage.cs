// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents one fetched documentation page.
/// </summary>
/// <param name="Url">The page URL.</param>
/// <param name="Markdown">The raw markdown of the page.</param>
public record DocsPage(PageUrl Url, string Markdown)
{
    /// <summary>Gets the page's explicit provenance, read from its own frontmatter and heading.</summary>
    public DocumentationMetadata Metadata => DocumentationMetadata.From(Url, Markdown);
}
