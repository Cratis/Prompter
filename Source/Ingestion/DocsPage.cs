// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents a documentation page fetched from the published site.
/// </summary>
/// <param name="Url">The URL of the page.</param>
/// <param name="Markdown">The markdown content of the page.</param>
public record DocsPage(PageUrl Url, string Markdown);
