// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

/// <summary>
/// v0.14.0 regression: a product list longer than the output cap was replaced by the generic
/// "couldn't compose" text, so the answer named no product at all.
/// </summary>
[Collection("Telemetry")]
public class and_the_output_cap_cuts_a_product_list : given.a_grounded_answerer
{
    readonly ILogger<Answers> _logger = Substitute.For<ILogger<Answers>>();
    readonly List<long> _outputLimitEvents = [];
    MeterListener _listener = null!;
    Answers _answerer = null!;

    void Establish()
    {
        Found(
            Passage(0.8),
            new Passage(new("https://cratis.io/arc/"), "Arc", "Overview", "CQRS", 0.02, 0.7),
            new Passage(new("https://cratis.io/components/"), "Components", "Overview", "UI", 0.01, 0.7));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                "{\"text\":\"- Chronicle: event store [1]\\n- Components: React UI [3]\\n- Arc: CQRS [2]\\n- Fundamentals: shar")) { FinishReason = ChatFinishReason.Length }));
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _answerer = new(passages, chat, interactions, Options.Create(options), _logger);
        _listener = new()
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (instrument.Meter.Name == ProviderMetrics.MeterName && instrument.Name == "prompter.events") owner.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "reason" && Equals(tag.Value, "output_limit")) _outputLimitEvents.Add(value);
            }
        });
        _listener.Start();
    }

    async Task Because() => result = await _answerer.For(new("What are all Cratis products and what do they do?"), "eval", TestContext.Current.CancellationToken);

    [Fact] void should_answer_with_the_complete_lines() => result.Text.ShouldEqual("- Chronicle: event store [1]\n- Components: React UI [3]\n- Arc: CQRS [2]\n…");
    [Fact] void should_cite_the_referenced_pages_in_order() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/chronicle/", "https://cratis.io/arc/", "https://cratis.io/components/"]);
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_record_a_single_non_refusal_interaction() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => !interaction.WasRefusal), Arg.Any<CancellationToken>());
    [Fact] void should_count_the_output_limit_once() => _outputLimitEvents.ShouldEqual([1L]);
    [Fact] void should_log_that_the_answer_was_salvaged() => OutputLimitWarning().Single(pair => pair.Key == "Salvaged").Value.ShouldEqual(true);

    void Destroy() => _listener.Dispose();

    KeyValuePair<string, object?>[] OutputLimitWarning() =>
        [.. _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == "Log" && Equals(call.GetArguments()[0], LogLevel.Warning))
            .Select(call => (IEnumerable<KeyValuePair<string, object?>>)call.GetArguments()[2]!)
            .Single(values => values.Any(pair => pair.Key == "Salvaged"))];
}
