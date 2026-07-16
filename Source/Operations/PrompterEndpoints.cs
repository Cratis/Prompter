// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Gateway;
using Npgsql;

namespace Cratis.Prompter.Operations;

/// <summary>
/// Maps the operational HTTP endpoints the bot hosts alongside the Discord gateway: a health probe and the
/// re-index webhook that keeps the corpus fresh without a redeploy.
/// </summary>
public static class PrompterEndpoints
{
    /// <summary>
    /// The request header carrying the shared secret that authorizes a re-index.
    /// </summary>
    public const string ReindexSecretHeader = "X-Reindex-Secret";

    const string ReindexLogCategory = "Cratis.Prompter.Operations.Reindex";

    /// <summary>
    /// Maps <c>GET /healthz</c> and <c>POST /reindex</c> onto the endpoint routing of the given application.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to map onto.</param>
    public static void MapPrompterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/healthz", CheckHealthAsync);
        endpoints.MapPost("/reindex", Reindex);
    }

    static async Task<IResult> CheckHealthAsync(
        NpgsqlDataSource dataSource,
        GatewayClient gateway,
        CancellationToken cancellationToken)
    {
        var databaseReachable = await IsDatabaseReachableAsync(dataSource, cancellationToken);
        var gatewayConnected = gateway.Status == WebSocketStatus.Ready;
        var healthy = databaseReachable && gatewayConnected;

        return Results.Json(
            new HealthStatus(healthy ? "healthy" : "unhealthy", databaseReachable, gatewayConnected),
            statusCode: healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable);
    }

    static IResult Reindex(
        HttpRequest request,
        IOptions<PrompterOptions> options,
        ReindexGate gate,
        IIndexer indexer,
        IHostApplicationLifetime lifetime,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(ReindexLogCategory);

        var providedSecret = request.Headers[ReindexSecretHeader].ToString();
        if (!ReindexAuth.IsAuthorized(providedSecret, options.Value.ReindexSecret))
        {
            logger.ReindexUnauthorized();
            return Results.Json(new { status = "unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!gate.TryEnter())
        {
            logger.ReindexAlreadyRunning();
            return Results.Json(new { status = "already running" }, statusCode: StatusCodes.Status409Conflict);
        }

        logger.ReindexStarted();

        // Fire-and-forget: the index run takes minutes, far longer than the HTTP request should live. It runs
        // under ApplicationStopping so a shutdown mid-run cancels it cleanly rather than being killed abruptly.
        // The gate is released in the finally so a failed or cancelled run never wedges future triggers, and
        // every outcome is logged.
        _ = Task.Run(async () =>
        {
            try
            {
                var run = await indexer.Run(lifetime.ApplicationStopping);
                logger.ReindexCompleted(run.Duration, run.Pages, run.Embedded, run.Unchanged, run.Removed);
            }
            catch (OperationCanceledException)
            {
                logger.ReindexCancelled();
            }
            catch (Exception exception)
            {
                logger.ReindexFailed(exception);
            }
            finally
            {
                gate.Exit();
            }
        });

        return Results.Json(new { status = "reindex started" }, statusCode: StatusCodes.Status202Accepted);
    }

    static async Task<bool> IsDatabaseReachableAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            // Any failure reaching the database means unhealthy; a health probe never surfaces the exception.
            return false;
        }
    }
}
