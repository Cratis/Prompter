// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Defines the published documentation site Prompter ingests from.
/// </summary>
public interface IDocsSite
{
    /// <summary>
    /// Gets all documentation pages from the site by walking its <c>llms.txt</c> index and fetching
    /// the markdown mirror of every page.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>An async stream of <see cref="DocsPage"/>.</returns>
    IAsyncEnumerable<DocsPage> GetPages(CancellationToken cancellationToken = default);
}
