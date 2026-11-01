// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reserves each batch before dispatch and releases only known unused exposure.</summary>
/// <param name="lease">The serialized index lease.</param>
/// <param name="options">The pricing and limits.</param>
/// <param name="request">The authorized run mode.</param>
public sealed class IndexMeter(IIndexLease lease, PrompterOptions options, IndexRequest request)
{
    /// <summary>Gets measured usage plus all in-flight or unknown exposure for this run.</summary>
    public IndexExposure Spent { get; private set; } = new(0, 0);

    /// <summary>Reserves the retry-inclusive batch bound before any dispatch.</summary>
    /// <param name="batch">The batch bound.</param>
    /// <param name="ct">Token for cancellation.</param>
    /// <returns>Awaitable task; refusal prevents dispatch.</returns>
    /// <exception cref="IndexBudgetExceeded">The batch would exceed the run ceiling.</exception>
    public async Task Reserve(IndexExposure batch, CancellationToken ct)
    {
        if (Spent.Dollars + batch.Dollars > IndexBudget.RunDollars(options.Ingestion, request)) throw new IndexBudgetExceeded();
        await lease.EnsureHeld(ct);
        await lease.Reserve(batch, IndexBudget.DayDollars(options.Ingestion, request), scheduled: false, full: request.Full, ct);
        Spent = new(checked(Spent.Tokens + batch.Tokens), Spent.Dollars + batch.Dollars);
    }

    /// <summary>Settles a successful batch; failed or cancelled dispatches must not call this.</summary>
    /// <param name="batch">The reserved bound.</param>
    /// <param name="used">The measured exposure including failed attempts.</param>
    /// <returns>False if the surplus could not be released and the bound was kept.</returns>
    public async Task<bool> Settle(IndexExposure batch, IndexExposure used)
    {
        var unused = new IndexExposure(batch.Tokens - used.Tokens, batch.Dollars - used.Dollars);
        if (unused.Tokens <= 0 || unused.Dollars <= 0) return true;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            await lease.Release(unused, timeout.Token);
            Spent = new(Spent.Tokens - unused.Tokens, Spent.Dollars - unused.Dollars);
            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
