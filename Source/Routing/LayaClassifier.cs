// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Prompter.Routing;

/// <summary>Private Decision API v1 routing, with no hosted or paid fallback.</summary>
/// <param name="client">A dedicated client with redirects disabled; production uses RoutingRegistration.</param>
/// <param name="options">Operator-owned private endpoint and uncalibrated thresholds.</param>
/// <param name="products">Keys from the configured routing map, never repositories.</param>
/// <param name="sink">Synchronous, nonthrowing metadata-only sink.</param>
public sealed class LayaClassifier(HttpClient client, RoutingOptions options, IReadOnlyList<string> products, IClassificationSink sink) : IRequestClassifier
{
    readonly RoutingProducts _products = new(products);
    readonly DecisionApi _api = new(client, options);
    int _sequence;
    int _inFlight;

    /// <inheritdoc/>
    public async Task<Classification> Classify(string currentRequest, IReadOnlyList<string> olderRequests, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var baseline = _products.Baseline(currentRequest[..Math.Min(currentRequest.Length, 4000)], ClassificationFallback.NotConfigured);
        if (options.Endpoint is null) return baseline;
        if (!PrivateDecisionEndpoint.IsAllowed(options)) return baseline with { Fallback = ClassificationFallback.EndpointRejected };
        if (options.Mode == ClassificationMode.Shadow && !ShouldSample()) return baseline with { Fallback = ClassificationFallback.Shadow };
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0) return baseline with { Fallback = ClassificationFallback.Busy };
        var started = Stopwatch.GetTimestamp();
        var observed = await WithinDeadline(ClassifierExcerpt.Build(currentRequest, olderRequests), baseline, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        sink.Record(new(options.Mode, observed.Intent, observed.Action, baseline.Action, observed.Confidence, observed.Fallback, Elapsed(started)));
        return options.Mode == ClassificationMode.Shadow ? baseline with { Fallback = ClassificationFallback.Shadow } : observed;
    }

    static ElapsedBucket Elapsed(long started) => Stopwatch.GetElapsedTime(started).TotalMilliseconds switch
    {
        < 100 => ElapsedBucket.Under100Milliseconds,
        < 500 => ElapsedBucket.Under500Milliseconds,
        < 1000 => ElapsedBucket.UnderOneSecond,
        _ => ElapsedBucket.Deadline
    };

    async Task<Classification> WithinDeadline(string excerpt, Classification baseline, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));
        try
        {
            // Keep ownership until even a noncooperative transport completes: later requests fall back,
            // rather than accumulating abandoned HTTP operations. Shadow itself is always awaited.
            return await CompleteOwned(excerpt, baseline, deadline.Token).WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return baseline with { Fallback = ClassificationFallback.Timeout };
        }
    }

    async Task<Classification> CompleteOwned(string excerpt, Classification baseline, CancellationToken cancellationToken)
    {
        try
        {
            return await _api.Decide(excerpt, DecisionChoices.Create(_products.Choices), baseline, cancellationToken);
        }
        catch (JsonException)
        {
            return baseline with { Fallback = ClassificationFallback.InvalidResponse };
        }
        catch (HttpRequestException)
        {
            return baseline with { Fallback = ClassificationFallback.Unavailable };
        }
        catch (IOException)
        {
            return baseline with { Fallback = ClassificationFallback.Unavailable };
        }
        catch (OperationCanceledException)
        {
            return baseline with { Fallback = ClassificationFallback.Timeout };
        }
        finally
        {
            Interlocked.Exchange(ref _inFlight, 0);
        }
    }

    bool ShouldSample()
    {
        var percentage = Math.Clamp(options.ShadowPercentage, 0, 10);
        var slot = (((uint)Interlocked.Increment(ref _sequence) - 1) % 100) + 1;
        return (slot * percentage / 100) != ((slot - 1) * percentage / 100);
    }
}
