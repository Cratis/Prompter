// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Specs.for_ProviderMetrics.when_recording;

[Collection("Telemetry")]
public class and_the_first_auth_failure_occurs : Specification
{
    [Fact]
    void should_expose_a_timestamp_without_requiring_a_previous_counter_sample()
    {
        var observed = new List<double>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == ProviderMetrics.MeterName && instrument.Name == "prompter.event.last_seen") owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "reason" && Equals(tag.Value, "auth")) observed.Add(value);
            }
        });
        listener.Start();

        ProviderMetrics.Event("anthropic", "auth");
        listener.RecordObservableInstruments();

        observed.ShouldNotBeEmpty();
        observed.ShouldEachConformTo(value => value > 0);
    }
}
