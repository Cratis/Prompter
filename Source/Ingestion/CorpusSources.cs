// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>Enumerates healthy sources while retaining failed repository snapshots.</summary>
/// <param name="docs">The published documentation.</param>
/// <param name="releaseNotes">The published release notes.</param>
/// <param name="samples">The curated samples.</param>
/// <param name="options">The source configuration.</param>
/// <param name="logger">Logger for source failures.</param>
public class CorpusSources(DocsSite docs, ReleaseNotes releaseNotes, SampleFiles samples, IOptions<PrompterOptions> options, ILogger<CorpusSources>? logger = null) : IDocsSite
{
    readonly List<string> _failed = [];

    /// <inheritdoc/>
    public IReadOnlyCollection<string> FailedSources => _failed;

    /// <inheritdoc/>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _failed.Clear();
        await foreach (var page in docs.GetPages(cancellationToken)) yield return page;
        var sources = new List<RepositorySource>();
        if (options.Value.Ingestion.ReleaseNotes.Enabled)
        {
            sources.Add(new("release notes", options.Value.Ingestion.ReleaseNotes.Repositories.Select(repository => $"https://github.com/Cratis/{repository}/releases/").ToArray(), releaseNotes.GetPages(cancellationToken)));
        }
        if (options.Value.Ingestion.Samples.Enabled)
        {
            sources.Add(new("samples", [$"https://github.com/{options.Value.Ingestion.Samples.Repository}/blob/"], samples.GetPages(cancellationToken)));
        }
        foreach (var source in sources)
        {
            foreach (var page in await Read(source, cancellationToken)) yield return page;
        }
    }

    static bool Recoverable(Exception error, CancellationToken cancellationToken) =>
        (error is IncompleteCrawl or HttpRequestException or TimeoutException) ||
        (error is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    async Task<IReadOnlyCollection<DocsPage>> Read(RepositorySource source, CancellationToken cancellationToken)
    {
        try
        {
            var pages = new List<DocsPage>();
            await foreach (var page in source.Pages.WithCancellation(cancellationToken)) pages.Add(page);
            if (pages.Count == 0) throw new IncompleteCrawl();
            return pages;
        }
        catch (Exception error) when (Recoverable(error, cancellationToken))
        {
            _failed.AddRange(source.Prefixes);
            (logger ?? NullLogger<CorpusSources>.Instance).RepositorySourceFailed(source.Name, error.GetType().Name);
            return [];
        }
    }

    sealed record RepositorySource(string Name, IReadOnlyCollection<string> Prefixes, IAsyncEnumerable<DocsPage> Pages);
}
