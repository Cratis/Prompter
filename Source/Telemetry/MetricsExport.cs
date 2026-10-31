// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Cratis.Prompter.Telemetry;

/// <summary>Exports only the explicit content-free meter, not activities, logs or HTTP requests.</summary>
public sealed class MetricsExport : IDisposable
{
    readonly MeterProvider? _provider;

    /// <summary>Initializes a new instance of the <see cref="MetricsExport"/> class.</summary>
    /// <param name="options">Opt-in private exporting.</param>
    /// <exception cref="OptionsValidationException">The enabled endpoint is not approved.</exception>
    public MetricsExport(IOptions<MetricsOptions> options)
    {
        if (!options.Value.Enabled) return;
        if (!PrivateMetricsEndpoint.IsAllowed(options.Value))
        {
            throw new OptionsValidationException(nameof(MetricsOptions), typeof(MetricsOptions), ["Private metrics endpoint is not approved."]);
        }

        _provider = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(CreateResource(options.Value.Environment))
            .AddMeter(ProviderMetrics.MeterName)
            .AddOtlpExporter((exporter, reader) =>
            {
                exporter.Endpoint = options.Value.Endpoint!;
                exporter.HttpClientFactory = CreateClient;
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                exporter.TimeoutMilliseconds = 3000;
                reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 30000;
            })
            .Build();
    }

    /// <summary>Creates the dedicated redirect-free, proxy-free OTLP transport.</summary>
    /// <returns>A bounded private HTTP client.</returns>
    public static HttpClient CreateClient()
    {
        var transport = CreateTransport();
        try
        {
            var client = new HttpClient(transport, disposeHandler: true) { Timeout = TimeSpan.FromSeconds(3) };
            transport = null; // Ownership has transferred to the client.
            return client;
        }
        finally
        {
            transport?.Dispose();
        }
    }

    /// <summary>Creates the exporter-only transport without redirect or proxy delegation.</summary>
    /// <returns>The private collector transport.</returns>
    public static SocketsHttpHandler CreateTransport() => new()
    {
        AllowAutoRedirect = false,
        UseProxy = false,
        ConnectTimeout = TimeSpan.FromSeconds(1)
    };

    /// <summary>
    /// Builds the content-free resource with a bounded deployment environment.
    /// </summary>
    /// <param name="environment">The configured deployment environment, never request metadata.</param>
    /// <returns>The resource builder used by the private exporter.</returns>
    public static ResourceBuilder CreateResource(string environment)
    {
        var boundedEnvironment = environment switch
        {
            "production" or "staging" or "development" or "test" => environment,
            _ => "unknown"
        };

        return ResourceBuilder.CreateEmpty()
            .AddService("prompter")
            .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", boundedEnvironment)]);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _provider?.ForceFlush(3000);
        _provider?.Dispose();
    }
}
