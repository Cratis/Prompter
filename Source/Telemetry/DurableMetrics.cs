// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Telemetry;

/// <summary>Exports durable CLI and background outcomes through the long-lived bot, never a push gateway.</summary>
public sealed class DurableMetrics : BackgroundService
{
    readonly NpgsqlDataSource _database;
    readonly Meter _meter = new(ProviderMetrics.MeterName);
    readonly Dictionary<string, double> _values = [];
    readonly Lock _lock = new();

    /// <summary>Initializes a new instance of the <see cref="DurableMetrics"/> class.</summary>
    /// <param name="database">The aggregate database.</param>
    /// <param name="options">Budget configuration.</param>
    /// <param name="export">The host-owned exporter.</param>
    public DurableMetrics(NpgsqlDataSource database, IOptions<PrompterOptions> options, MetricsExport export)
    {
        _database = database;
        _ = export;
        foreach (var signal in new[] { "attempts", "successes", "failures", "cancellations", "last_attempt", "last_success", "last_failure", "duration_seconds", "documents", "chunks", "anthropic_usd", "voyage_usd", "index_usd", "updated_at" })
        {
            _meter.CreateObservableGauge($"prompter.state.{signal}", () => Value(signal));
        }

        _meter.CreateObservableGauge("prompter.limit.daily_usd", () => (double)options.Value.Protection.DailyUsd);
        _meter.CreateObservableGauge("prompter.limit.index_daily_usd", () => (double)options.Value.Ingestion.MaxDailyDollars);
        _meter.CreateObservableGauge("prompter.limit.anthropic_daily_usd", () => (double)options.Value.Protection.Anthropic.DailyUsd);
        _meter.CreateObservableGauge("prompter.limit.voyage_daily_usd", () => (double)options.Value.Protection.Voyage.DailyUsd);
        _meter.CreateObservableGauge("prompter.quota.primary_reset", () => options.Value.Anthropic.PrimaryQuotaResetAt?.ToUnixTimeSeconds() ?? 0);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _meter.Dispose();
        base.Dispose();
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Refresh(stoppingToken);
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    async Task Refresh(CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await using var command = _database.CreateCommand("""
                SELECT attempts, successes, failures, cancellations,
                    COALESCE(EXTRACT(EPOCH FROM last_attempt), 0) AS last_attempt,
                    COALESCE(EXTRACT(EPOCH FROM last_success), 0) AS last_success,
                    COALESCE(EXTRACT(EPOCH FROM last_failure), 0) AS last_failure,
                    duration_seconds, documents, chunks,
                    COALESCE((SELECT anthropic FROM prompter_spend_days WHERE day = (now() AT TIME ZONE 'UTC')::date), 0) AS anthropic_usd,
                    COALESCE((SELECT voyage FROM prompter_spend_days WHERE day = (now() AT TIME ZONE 'UTC')::date), 0) AS voyage_usd,
                    COALESCE((SELECT dollars FROM index_reservations WHERE day = (now() AT TIME ZONE 'UTC')::date), 0) AS index_usd
                FROM prompter_index_health WHERE singleton
                """);
            await using var reader = await command.ExecuteReaderAsync(timeout.Token);
            if (!await reader.ReadAsync(timeout.Token)) return;
            lock (_lock)
            {
                for (var index = 0; index < reader.FieldCount; index++) _values[reader.GetName(index)] = Convert.ToDouble(reader.GetValue(index), System.Globalization.CultureInfo.InvariantCulture);
                _values["updated_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }
        catch (Exception error) when (error is NpgsqlException or OperationCanceledException or TimeoutException)
        {
            ProviderMetrics.Event("index", "telemetry_failure");
        }
    }

    IEnumerable<Measurement<double>> Value(string signal)
    {
        lock (_lock)
        {
            return _values.TryGetValue(signal, out var value) ? [new(value)] : [];
        }
    }
}
