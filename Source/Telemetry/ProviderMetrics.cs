// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Telemetry;

/// <summary>Content-free, fixed-cardinality measurements. No automatic GenAI or HTTP instrumentation.</summary>
public static class ProviderMetrics
{
    /// <summary>The only exported instrumentation scope.</summary>
    public const string MeterName = "Cratis.Prompter";

    static readonly Meter _meter = new(MeterName);
    static readonly Counter<long> _attempts = _meter.CreateCounter<long>("prompter.provider.attempts");
    static readonly Counter<long> _operations = _meter.CreateCounter<long>("prompter.provider.operations");
    static readonly Counter<long> _tokens = _meter.CreateCounter<long>("prompter.provider.tokens");
    static readonly Counter<long> _usage = _meter.CreateCounter<long>("prompter.provider.usage");
    static readonly Counter<double> _cost = _meter.CreateCounter<double>("prompter.provider.estimated_usd");
    static readonly Histogram<double> _duration = _meter.CreateHistogram<double>("prompter.provider.duration", "s");
    static readonly Counter<long> _events = _meter.CreateCounter<long>("prompter.events");
    static readonly Histogram<double> _backoff = _meter.CreateHistogram<double>("prompter.provider.backoff", "s");
    static readonly Lock _eventLock = new();
    static readonly Dictionary<(string Source, string Reason), double> _lastEvents = [];
    static readonly HashSet<string> _cacheCreationKeys = new(StringComparer.Ordinal) { "CacheCreationInputTokens", "CacheCreationInputTokenCount", "cache_creation_input_tokens" };

    static ProviderMetrics() => _meter.CreateObservableGauge("prompter.event.last_seen", LastEvents);

    /// <summary>Builds the complete allowlisted provider label set; never emits operation identities.</summary>
    /// <param name="dispatch">The accounting metadata.</param>
    /// <param name="outcome">The bounded result category.</param>
    /// <returns>Only fixed keys and fixed values.</returns>
    public static TagList Labels(ProviderDispatch dispatch, string outcome) => new()
    {
        { "provider", Allow(dispatch.Provider, "anthropic", "voyage") },
        { "model", Allow(dispatch.Model, "claude-sonnet-5-5", "claude-haiku-4-5", "claude-haiku-4-5-20251001", "voyage-4") },
        { "task", Allow(dispatch.Task, "answer", "issue_draft", "query", "index") },
        { "account", Allow(dispatch.Account, "primary", "secondary") },
        { "operation", dispatch.Provider switch { "anthropic" => "chat", "voyage" => "embedding", _ => "other" } },
        { "outcome", Allow(outcome, "success", "refusal", "unknown", "cancelled", "capacity", "rate_limit", "auth", "quota", "provider_failure", "denied") }
    };

    /// <summary>Records dispatch counts separately from logical operations and retries.</summary>
    /// <param name="dispatch">The accounting metadata.</param>
    public static void Begin(ProviderDispatch dispatch)
    {
        var tags = Labels(dispatch, "unknown");
        _attempts.Add(1, tags);
        if (dispatch.Attempt == 1) _operations.Add(1, tags);
    }

    /// <summary>Records known use only; unknown use is not zero and never refunds exposure.</summary>
    /// <param name="dispatch">The accounting metadata.</param>
    /// <param name="result">Known usage and sanitized attempt outcome.</param>
    /// <param name="prices">Verified upper prices.</param>
    public static void Complete(ProviderDispatch dispatch, ProviderResult result, ProviderBudget prices)
    {
        var tags = Labels(dispatch, result.Outcome);
        _duration.Record(result.Duration.TotalSeconds, tags);
        if (result.Outcome != "success") Event(dispatch.Provider, result.Outcome);
        var usage = result.Usage;
        var known = IsKnown(dispatch, usage);
        var usageTags = tags;
        usageTags.Add("knowledge", known ? "known" : "unknown");
        _usage.Add(1, usageTags);
        if (!known) return;
        RecordUsage(usage!, tags);
        RecordCost(dispatch, usage!, prices, tags);
    }

    /// <summary>Records only independently bounded operational categories.</summary>
    /// <param name="source">A fixed subsystem name.</param>
    /// <param name="reason">A fixed operational reason.</param>
    public static void Event(string source, string reason)
    {
        var safeSource = Allow(source, "anthropic", "voyage", "admission", "protection", "index");
        var safeReason = Allow(reason, "refusal", "cancelled", "capacity", "capacity_exhausted", "rate_limit", "auth", "quota", "provider_failure", "denied", "concurrency", "budget", "pricing", "disabled", "hourly", "retry", "fallback_quota", "cooldown", "telemetry_failure");
        _events.Add(1, new KeyValuePair<string, object?>("source", safeSource), new KeyValuePair<string, object?>("reason", safeReason));
        lock (_eventLock) _lastEvents[(safeSource, safeReason)] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>Records a bounded retry delay, never an HTTP header.</summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="delay">The selected wait.</param>
    public static void Retry(string provider, TimeSpan delay)
    {
        Event(provider, "retry");
        _backoff.Record(Math.Clamp(delay.TotalSeconds, 0, 120), new KeyValuePair<string, object?>("provider", Allow(provider, "anthropic", "voyage")));
    }

    /// <summary>Classifies by types and HTTP status only, never error text.</summary>
    /// <param name="error">The failure, whose message is never inspected.</param>
    /// <returns>A bounded category.</returns>
    public static string Category(Exception error) => error switch
    {
        OperationCanceledException => "cancelled",
        WorkRefused => "denied",
        AnthropicCapacityFailure => "capacity",
        ProviderUnavailable failure => failure.Category,
        HttpRequestException failure => HttpCategory(failure.StatusCode),
        _ => "provider_failure"
    };

    /// <summary>Maps only safe HTTP status codes shared by both transports.</summary>
    /// <param name="status">The response status, if available.</param>
    /// <returns>A fixed category.</returns>
    internal static string HttpCategory(HttpStatusCode? status) => status switch
    {
        HttpStatusCode.TooManyRequests => "rate_limit",
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "auth",
        HttpStatusCode.PaymentRequired => "quota",
        _ => "provider_failure"
    };

    static IEnumerable<Measurement<double>> LastEvents()
    {
        lock (_eventLock)
        {
            return _lastEvents.Select(item => new Measurement<double>(
                item.Value,
                new KeyValuePair<string, object?>("source", item.Key.Source),
                new KeyValuePair<string, object?>("reason", item.Key.Reason))).ToArray();
        }
    }

    static bool IsKnown(ProviderDispatch dispatch, UsageDetails? usage) =>
        usage is { InputTokenCount: >= 0 } && (dispatch.Provider == "voyage" || usage.OutputTokenCount >= 0);

    static void RecordUsage(UsageDetails usage, TagList tags)
    {
        RecordTokens(usage.InputTokenCount, "input", tags);
        RecordTokens(usage.OutputTokenCount, "output", tags);
        RecordTokens(usage.CachedInputTokenCount, "cache_read", tags);
        RecordTokens(usage.ReasoningTokenCount, "reasoning", tags);
        if (usage.AdditionalCounts is not { } additional) return;
        var creation = additional.FirstOrDefault(count => _cacheCreationKeys.Contains(count.Key));
        if (creation.Key is not null) RecordTokens(creation.Value, "cache_creation", tags);
    }

    static void RecordCost(ProviderDispatch dispatch, UsageDetails usage, ProviderBudget prices, TagList tags)
    {
        if (prices.Covers(dispatch.Model, dispatch.Provider == "anthropic") && usage.AdditionalCounts?.Values.All(count => count == 0) != false)
        {
            _cost.Add((double)prices.Cost(usage.InputTokenCount!.Value, usage.OutputTokenCount ?? 0), tags);
        }
    }

    static string Allow(string value, params string[] allowed) => allowed.Contains(value, StringComparer.Ordinal) ? value : "other";

    static void RecordTokens(long? value, string kind, TagList tags)
    {
        if (value is not >= 0) return;
        tags.Add("kind", kind);
        _tokens.Add(value.Value, tags);
    }
}
