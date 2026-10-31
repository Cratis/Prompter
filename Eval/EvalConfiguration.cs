// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Eval;

/// <summary>Eval-only settings; no production configuration or CLI credential binding.</summary>
internal sealed record EvalConfiguration
{
    /// <summary>Gets the isolated disposable database connection.</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>Gets the dedicated eval answer/judge credential.</summary>
    public string AnthropicApiKey { get; init; } = string.Empty;

    /// <summary>Gets the dedicated eval embedding credential.</summary>
    public string VoyageApiKey { get; init; } = string.Empty;

    /// <summary>Gets the pinned answer model.</summary>
    public string AnswerModel { get; init; } = string.Empty;

    /// <summary>Gets the pinned judge model.</summary>
    public string JudgeModel { get; init; } = string.Empty;

    /// <summary>Gets the pinned embedding model.</summary>
    public string EmbeddingModel { get; init; } = string.Empty;

    /// <summary>Gets the isolated corpus manifest.</summary>
    public string CorpusManifest { get; init; } = string.Empty;

    /// <summary>Gets whether an operator explicitly approved paid execution.</summary>
    public bool PaidApproved { get; init; }

    /// <summary>Gets the fail-closed reason; paid execution is not yet integrated.</summary>
    public string PaidBlockReason
    {
        get
        {
            if (new[] { ConnectionString, AnthropicApiKey, VoyageApiKey, AnswerModel, JudgeModel, EmbeddingModel, CorpusManifest }.Any(string.IsNullOrWhiteSpace))
            {
                return "Missing eval-only settings.";
            }
            if (!PaidApproved)
            {
                return "Explicit paid approval is required.";
            }
            return "Paid evaluation is disabled: aggregate budget/admission integration is pending.";
        }
    }

    /// <summary>Reads exclusively the eval section of supplied configuration.</summary>
    /// <param name="configuration">Configuration, typically synthetic in specifications.</param>
    /// <returns>Eval-only settings, with no inherited production defaults.</returns>
    public static EvalConfiguration From(IConfiguration configuration) =>
        configuration.GetSection("Cratis:PrompterEval").Get<EvalConfiguration>() ?? new();
}
