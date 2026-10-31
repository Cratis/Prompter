// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.for_ProviderMetrics.when_recording;

[Collection("Telemetry")]
public class and_usage_is_known : Specification
{
    [Fact]
    void should_record_input_output_and_cache_subsets_without_request_metadata()
    {
        var counts = new Dictionary<string, long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == ProviderMetrics.MeterName && instrument.Name == "prompter.provider.tokens") owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "kind") counts[(string)tag.Value!] = value;
            }
        });
        listener.Start();

        ProviderMetrics.Complete(
            new(Guid.NewGuid(), "anthropic", "primary", "answer", 1, 100, 100, "claude-sonnet-5-5", "high"),
            new(
                new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50, CachedInputTokenCount = 20, ReasoningTokenCount = 10, AdditionalCounts = new() { ["cache_creation_input_tokens"] = 5 } },
                "success",
                TimeSpan.FromSeconds(1)),
            new());

        counts["input"].ShouldEqual(100);
        counts["output"].ShouldEqual(50);
        counts["cache_read"].ShouldEqual(20);
        counts["reasoning"].ShouldEqual(10);
        counts["cache_creation"].ShouldEqual(5);
    }
}
