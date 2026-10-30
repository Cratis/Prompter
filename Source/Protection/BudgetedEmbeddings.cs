// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>Reserves before each embedding attempt, inside the retry decorator, including CLI indexing.</summary>
/// <param name="inner">The nonretrying Voyage transport.</param>
/// <param name="ledger">The shared daily ledger.</param>
/// <param name="configuration">Verified model limits and prices.</param>
public sealed class BudgetedEmbeddings(
    IEmbeddingGenerator<string, Embedding<float>> inner,
    ISpendLedger ledger,
    IOptions<PrompterOptions> configuration) : DelegatingEmbeddingGenerator<string, Embedding<float>>(inner)
{
    /// <inheritdoc/>
    public override async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var config = configuration.Value;
        var budget = config.Protection.Voyage;
        var input = values.ToArray();
        var bytes = input.Sum(value => (long)Encoding.UTF8.GetByteCount(value));
        var lease = WorkLease.Current;
        CheckDispatch(bytes, options, lease is not null);

        // Reserve the verified hard per-input model ceiling, including tokenizer/framing overhead.
        var cost = budget.Cost((long)budget.MaxInputTokens * input.Length);
        if (lease is not null)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.Token);
            lease.DispatchEmbedding(cost);
            return await base.GenerateAsync(input, options, linked.Token);
        }

        // No interactive context: CLI/index batches reserve independently, before each transport retry.
        await Reserve(cost, cancellationToken);
        return await base.GenerateAsync(input, options, cancellationToken);
    }

    async Task Reserve(decimal cost, CancellationToken cancellationToken)
    {
        try
        {
            _ = await ledger.Reserve(new(0, cost), cancellationToken) ?? throw new WorkRefused();
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException)
        {
            throw new WorkRefused();
        }
    }

    void CheckDispatch(long bytes, EmbeddingGenerationOptions? request, bool interactive)
    {
        var config = configuration.Value;
        var budget = config.Protection.Voyage;
        if (!budget.Covers(config.Voyage.Model, false))
        {
            throw new WorkRefused();
        }

        if (bytes <= 0 || bytes > budget.MaxInputTokens)
        {
            throw new WorkRefused();
        }

        var enabled = interactive ? config.Protection.AnsweringEnabled : config.Protection.IndexingEnabled;
        if (!enabled || !string.Equals(request?.ModelId ?? config.Voyage.Model, budget.Model, StringComparison.Ordinal))
        {
            throw new WorkRefused();
        }
    }
}
