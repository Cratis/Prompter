// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>Enumerates every enabled source in one atomic index run.</summary>
/// <param name="docs">The published documentation.</param>
/// <param name="releaseNotes">The published release notes.</param>
/// <param name="options">The source configuration.</param>
public class CorpusSources(DocsSite docs, ReleaseNotes releaseNotes, IOptions<PrompterOptions> options) : IDocsSite
{
    /// <inheritdoc/>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sources = new List<IAsyncEnumerable<DocsPage>> { docs.GetPages(cancellationToken) };
        if (options.Value.Ingestion.ReleaseNotes.Enabled)
        {
            sources.Add(releaseNotes.GetPages(cancellationToken));
        }

        foreach (var source in sources)
        {
            await foreach (var page in source.WithCancellation(cancellationToken))
            {
                yield return page;
            }
        }
    }
}
