// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.GitHub.Authentication;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub;

/// <summary>Two bounded GET searches sharing one second, with no retry, cache, or issue-body requests.</summary>
/// <param name="httpClient">The redirect-free, request-authenticated GitHub client.</param>
/// <param name="options">The explicit product mapping and installation scope.</param>
/// <param name="clock">The closed-window clock.</param>
public sealed class RelatedIssues(HttpClient httpClient, IOptions<PrompterOptions> options, TimeProvider clock) : IRelatedIssues
{
    const int MaximumResponseBytes = 65536;
    readonly GitHubPolicy _policy = new(options);

    /// <inheritdoc/>
    public Task<IReadOnlyList<RelatedIssue>> FindRelated(string product, string currentRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return options.Value.GitHub.Repositories.TryGetValue(product, out var repository)
            ? Search(repository, currentRequest, includeClosed: true, cancellationToken)
            : Task.FromResult<IReadOnlyList<RelatedIssue>>([]);
    }

    /// <summary>Compatibility lookup for the explicit issue preview; remains open-only.</summary>
    /// <param name="repository">An installation-allowlisted repository.</param>
    /// <param name="title">The draft's current title, never its body or history.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>At most three canonical open hints.</returns>
    public Task<IReadOnlyList<RelatedIssue>> FindOpen(string repository, string title, CancellationToken cancellationToken = default) =>
        Search(repository, title, includeClosed: false, cancellationToken);

    async Task<IReadOnlyList<RelatedIssue>> Search(string repository, string text, bool includeClosed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.GitHub.FilingEnabled) return [];
        var keywords = RelatedKeywords.From(text);
        if (keywords.Length == 0) return [];
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));
        try
        {
            var destination = new GitHubDestination(options.Value.GitHub.Owner, repository);
            _policy.RequireRepository(destination);
            var since = clock.GetUtcNow().AddDays(-90);
            var open = await Bucket(new(destination, false, since), keywords, deadline.Token);
            if (!includeClosed) return open;
            var closed = await Bucket(new(destination, true, since), keywords, deadline.Token);
            return [.. open.Concat(closed).DistinctBy(issue => issue.Number).Take(6)];
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return [];
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or GitHubAuthorizationUnavailable or InvalidOperationException)
        {
            // No remote error text, query, identity, or credential is logged or sent back to the model.
            return [];
        }
    }

    async Task<IReadOnlyList<RelatedIssue>> Bucket(RelatedSearchResponse bucket, string keywords, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"search/issues?q={Uri.EscapeDataString(bucket.Query(keywords))}&per_page=3", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumResponseBytes) return [];
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var bounded = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (bounded.Length + read > MaximumResponseBytes) return [];
            await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        using var document = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        return bucket.Read(document.RootElement);
    }
}
