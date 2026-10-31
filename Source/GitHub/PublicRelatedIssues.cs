// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Anonymous GET-only public issue hints: at most two searches, a combined two-second deadline, no retry,
/// a bounded in-memory cache, and no credentials or write policy involved. Issue text is untrusted and never retained.
/// </summary>
/// <param name="httpClient">The anonymous, redirect-free client for the GitHub API.</param>
/// <param name="options">The Prompter options carrying the off switch.</param>
/// <param name="clock">The clock for the closed window, cache and limiter.</param>
/// <param name="limits">The process-wide limiter.</param>
public sealed class PublicRelatedIssues(HttpClient httpClient, IOptions<PrompterOptions> options, TimeProvider clock, PublicIssueSearchLimits limits) : IPublicRelatedIssues
{
    const int MaximumLinks = 3;
    const int MaximumCacheEntries = 128;
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(2);
    static readonly TimeSpan _timeToLive = TimeSpan.FromMinutes(15);
    static readonly Meter _meter = new(Telemetry.ProviderMetrics.MeterName);
    static readonly Counter<long> _degraded = _meter.CreateCounter<long>("prompter_public_related_degraded_total");

    readonly Dictionary<string, (DateTimeOffset Expires, IReadOnlyList<RelatedIssue> Issues)> _cache = [];
    int _running;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RelatedIssue>> Find(string currentRequest, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.GitHub.PublicRelatedIssuesEnabled)
        {
            return [];
        }

        var keywords = RelatedKeywords.From(currentRequest);
        if (keywords.Length == 0 || Interlocked.CompareExchange(ref _running, 1, 0) != 0)
        {
            return [];
        }

        try
        {
            return await Lookup(keywords.ToLowerInvariant(), cancellationToken);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    static string Query(string keywords, bool closed, DateTimeOffset since)
    {
        var repositories = string.Join(' ', PublicSearchResponse.Repositories.Select(repository => $"repo:{PublicSearchResponse.Owner}/{repository}"));
        var state = closed ? $"is:closed closed:>={since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" : "is:open";
        return $"is:issue is:public {repositories} {state} {keywords}";
    }

    static IReadOnlyList<RelatedIssue> Degraded(string reason)
    {
        _degraded.Add(1, new KeyValuePair<string, object?>("reason", reason));
        return [];
    }

    async Task<IReadOnlyList<RelatedIssue>> Lookup(string keywords, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        if (_cache.TryGetValue(keywords, out var cached) && cached.Expires > now)
        {
            return cached.Issues;
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_deadline);
        try
        {
            var since = now.AddDays(-90);
            var open = await Bucket(keywords, closed: false, since, deadline.Token);
            var closed = open is null ? null : await Bucket(keywords, closed: true, since, deadline.Token);
            if (open is null || closed is null)
            {
                return [];
            }

            IReadOnlyList<RelatedIssue> issues = [.. open.Concat(closed).DistinctBy(issue => issue.Url).Take(MaximumLinks)];
            Remember(keywords, now, issues);
            return issues;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Degraded("deadline");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            // No query, response or remote text is logged.
            return Degraded("failure");
        }
    }

    async Task<IReadOnlyList<RelatedIssue>?> Bucket(string keywords, bool closed, DateTimeOffset since, CancellationToken cancellationToken)
    {
        if (!limits.TryAcquire())
        {
            Degraded("limited");
            return null;
        }

        using var response = await httpClient.GetAsync($"search/issues?q={Uri.EscapeDataString(Query(keywords, closed, since))}&per_page=6", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests)
        {
            limits.CoolDown(PublicSearchHttp.RetryAfter(response, clock.GetUtcNow()));
            Degraded("rate-limited");
            return null;
        }

        if (!response.IsSuccessStatusCode || await PublicSearchHttp.Bounded(response, cancellationToken) is not { } body)
        {
            Degraded("unavailable");
            return null;
        }

        using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 16 });
        var issues = PublicSearchResponse.Read(document.RootElement, closed, since, clock.GetUtcNow());
        if (issues is null)
        {
            Degraded("malformed");
        }

        return issues;
    }

    void Remember(string keywords, DateTimeOffset now, IReadOnlyList<RelatedIssue> issues)
    {
        if (_cache.Count >= MaximumCacheEntries)
        {
            foreach (var expired in _cache.Where(entry => entry.Value.Expires <= now).Select(entry => entry.Key).ToArray())
            {
                _cache.Remove(expired);
            }

            if (_cache.Count >= MaximumCacheEntries)
            {
                _cache.Remove(_cache.MinBy(entry => entry.Value.Expires).Key);
            }
        }

        _cache[keywords] = (now + _timeToLive, issues);
    }
}
