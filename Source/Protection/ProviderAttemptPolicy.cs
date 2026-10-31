// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Protection;

/// <summary>Closed metadata validation shared by permit funding and usage reconciliation.</summary>
internal static class ProviderAttemptPolicy
{
    static readonly HashSet<string> _providers = new(StringComparer.Ordinal) { "anthropic", "voyage" };
    static readonly HashSet<string> _accounts = new(StringComparer.Ordinal) { "primary", "secondary" };
    static readonly HashSet<string> _chatTasks = new(StringComparer.Ordinal) { "answer", "issue-draft" };
    static readonly HashSet<string> _embeddingTasks = new(StringComparer.Ordinal) { "query", "index" };
    static readonly HashSet<string> _efforts = new(StringComparer.Ordinal) { string.Empty, "low", "medium", "high" };

    /// <summary>Validates closed metadata and computes the same conservative bound as the inner decorator.</summary>
    /// <param name="dispatch">The requested attempt.</param>
    /// <param name="count">Batch size.</param>
    /// <param name="config">Verified prices and policy.</param>
    /// <returns>The conservative cost.</returns>
    internal static decimal Upper(ProviderDispatch dispatch, int count, PrompterOptions config)
    {
        Require(dispatch.OperationId != Guid.Empty);
        Require(_providers.Contains(dispatch.Provider));
        Require(count is >= 1 and <= EmbeddingInputLimits.MaximumItems);
        Require(config.Protection.IsValid);
        Require(config.Protection.DailyUsd is > 0 and <= 5);
        var chat = dispatch.Provider == "anthropic";
        var budget = chat ? config.Protection.Anthropic : config.Protection.Voyage;
        var model = chat ? config.Anthropic.Model : config.Voyage.Model;
        Require(budget.Covers(model, chat) && dispatch.Model == model);
        var inputCeiling = dispatch.Task == "index" && !chat
            ? Math.Min(EmbeddingInputLimits.MaximumRequestTokens, (long)EmbeddingInputLimits.PerInput(budget.MaxInputTokens) * count)
            : budget.MaxInputTokens;
        Require(dispatch.MaxInputTokens >= 1 && dispatch.MaxInputTokens <= inputCeiling);
        Require(budget.DailyUsd <= config.Protection.DailyUsd);
        if (chat)
        {
            Chat(dispatch, count, config);
        }
        else
        {
            Embedding(dispatch, config);
        }

        return budget.Cost((long)budget.MaxInputTokens * count, dispatch.MaxOutputTokens);
    }

    /// <summary>Denies invalid policy without authorizing transport.</summary>
    /// <param name="condition">The required condition.</param>
    /// <exception cref="WorkRefused">The condition is not satisfied.</exception>
    internal static void Require(bool condition)
    {
        if (!condition)
        {
            throw new WorkRefused();
        }
    }

    static void Chat(ProviderDispatch dispatch, int count, PrompterOptions config)
    {
        Require(config.Protection.AnsweringEnabled && count == 1);
        Require(dispatch.Attempt is >= 1 and <= 2);
        Require(_chatTasks.Contains(dispatch.Task));
        var policy = dispatch.Task == "answer" ? config.Anthropic.Answer : config.Anthropic.IssueDraft;
        Require(_accounts.Contains(dispatch.Account));
        Require(dispatch.Account != "secondary" || policy.AllowSecondary);
        Require(_accounts.Contains(policy.PreferredAccount));
        Require(dispatch.MaxInputTokens <= policy.MaxPromptTokens);
        Require(dispatch.MaxOutputTokens >= 1 && dispatch.MaxOutputTokens <= Math.Min(policy.MaxOutputTokens, config.Answering.MaxOutputTokens));
        Require(dispatch.Effort == policy.Effort);
        Require(_efforts.Contains(dispatch.Effort));
    }

    static void Embedding(ProviderDispatch dispatch, PrompterOptions config)
    {
        Require(dispatch.Account == "primary");
        Require(_embeddingTasks.Contains(dispatch.Task));
        Require(dispatch.Effort.Length == 0 && dispatch.MaxOutputTokens == 0);
        var indexing = dispatch.Task == "index";
        Require(!indexing || WorkLease.Current is null);
        Require(indexing ? config.Protection.IndexingEnabled : config.Protection.AnsweringEnabled);
        Require(dispatch.Attempt >= 1 && dispatch.Attempt <= EmbeddingRetryPolicy.Attempts(config.Voyage, indexing));
    }
}
