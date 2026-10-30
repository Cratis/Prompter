// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Specs.for_ProviderMetrics.when_recording;

[Collection("Telemetry")]
public class and_usage_is_unknown : Specification
{
    [Fact]
    void should_not_report_unknown_usage_as_zero_tokens()
    {
        var tokens = new List<long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == ProviderMetrics.MeterName && instrument.Name == "prompter.provider.tokens") owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => tokens.Add(value));
        listener.Start();

        ProviderMetrics.Complete(new(Guid.NewGuid(), "voyage", "primary", "index", 1, 10, 0, "voyage-4", string.Empty), new(null, "success", TimeSpan.Zero), new());

        tokens.ShouldBeEmpty();
    }
}
