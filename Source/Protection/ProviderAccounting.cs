// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>Funds each attempt before transport; unknown or unpriced usage retains its entire bound.</summary>
/// <param name="store">Transactional attempt persistence.</param>
/// <param name="options">Verified prices and bounded task policies.</param>
public sealed class ProviderAccounting(IProviderAttemptStore store, IOptions<PrompterOptions> options) : IProviderAccounting
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
            var cost = KnownCost(dispatch, scope.InputCount, usage);
            if (await store.Complete(dispatch, cost, cleanup.Token))
            {
                scope.Completed(new(cost));
            }
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or OperationCanceledException or OverflowException or WorkRefused)
        {
            // Failed durable completion never authorizes a refund of the live prepaid envelope.
        }
    }

    static bool SupportedUsage(UsageDetails? usage) =>
        usage?.InputTokenCount is >= 0 && usage.AdditionalCounts?.Values.Any(value => value != 0) != true;

    static bool WithinBounds(ProviderDispatch dispatch, UsageDetails usage, long inputCeiling)
    {
        var input = usage.InputTokenCount!.Value;
        var output = usage.OutputTokenCount ?? (dispatch.Provider == "anthropic" ? -1 : 0);
        var tokenBounds = input <= inputCeiling && output >= 0 && output <= dispatch.MaxOutputTokens;
        var totalMatches = usage.TotalTokenCount is null || usage.TotalTokenCount == input + output;
        return tokenBounds && totalMatches;
    }

    decimal? KnownCost(ProviderDispatch dispatch, int count, UsageDetails? usage)
    {
        var upper = ProviderAttemptPolicy.Upper(dispatch, count, options.Value);
        if (!SupportedUsage(usage))
        {
            return null;
        }

        var chat = dispatch.Provider == "anthropic";
        var budget = chat ? ChatPricing.ForTask(options.Value, dispatch.Task) : options.Value.Protection.Voyage;
        var input = usage!.InputTokenCount!.Value;
        var output = usage.OutputTokenCount ?? (chat ? -1 : 0);
        if (!WithinBounds(dispatch, usage, (long)budget.MaxInputTokens * count))
        {
            return null;
        }

        var cost = budget.Cost(input, output);
        return cost <= upper ? cost : null;
    }
}
