// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>Funds each attempt before transport; unknown or unpriced usage retains its entire bound.</summary>
/// <param name="store">Transactional attempt persistence.</param>
/// <param name="options">Verified prices and bounded task policies.</param>
/// <param name="logger">Logs why a settlement was rejected.</param>
public sealed class ProviderAccounting(IProviderAttemptStore store, IOptions<PrompterOptions> options, ILogger<ProviderAccounting> logger) : IProviderAccounting
{
    /// <inheritdoc/>
    public async Task<bool> Reserve(ProviderDispatch dispatch, CancellationToken cancellationToken)
    {
        var scope = ProviderAttemptScope.Current;
        if (scope?.BeginReservation(dispatch) != true)
        {
            return false;
        }

        try
        {
            var upper = ProviderAttemptPolicy.Upper(dispatch, scope.InputCount, options.Value);
            var prepaid = scope.Parent is not null && (dispatch.Provider == "voyage" || dispatch.Attempt == 1);
            var parent = prepaid ? scope.Parent : null;
            if (!await store.Reserve(dispatch, parent, upper, cancellationToken))
            {
                return false;
            }

            scope.Grant(new(upper, prepaid));
            return true;
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or WorkRefused or OverflowException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task Complete(ProviderDispatch dispatch, UsageDetails? usage, string finishReason)
    {
        var scope = ProviderAttemptScope.Current;
        if (scope is null || scope.Dispatch != dispatch)
        {
            return;
        }

        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var (cost, rejection) = KnownCost(dispatch, scope.InputCount, usage);
            if (rejection is { } reason)
            {
                logger.SettlementRejected(dispatch.Provider, dispatch.Task, dispatch.Attempt, reason);
            }

            if (await store.Complete(dispatch, cost, cleanup.Token))
            {
                scope.Completed(new(cost));
                if (!scope.Prepaid)
                {
                    logger.SettledOutsideEnvelope(dispatch.Attempt, dispatch.Provider, dispatch.Task);
                }
            }
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or OperationCanceledException or OverflowException or WorkRefused)
        {
            // Failed durable completion never authorizes a refund of the live prepaid envelope.
        }
    }

    (decimal? Cost, SettlementRejection? Rejection) KnownCost(ProviderDispatch dispatch, int count, UsageDetails? usage)
    {
        var chat = dispatch.Provider == "anthropic";
        var budget = chat ? ChatPricing.ForTask(options.Value, dispatch.Task) : options.Value.Protection.Voyage;
        var (cost, rejection) = UsagePrice.Of(budget, usage, chat);
        if (cost is null)
        {
            return (null, rejection);
        }

        var input = usage!.InputTokenCount!.Value;
        var output = usage.OutputTokenCount ?? 0;
        if (input > (long)budget.MaxInputTokens * count)
        {
            return (null, SettlementRejection.InputAboveCeiling);
        }

        if (output > dispatch.MaxOutputTokens)
        {
            return (null, SettlementRejection.OutputInvalid);
        }

        if (usage.TotalTokenCount is not null && usage.TotalTokenCount != input + output)
        {
            return (null, SettlementRejection.TotalMismatch);
        }

        return cost <= ProviderAttemptPolicy.Upper(dispatch, count, options.Value) ? (cost, null) : (null, SettlementRejection.CostAboveUpper);
    }
}
