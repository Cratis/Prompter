// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi.Kubernetes.Types.Inputs.Core.V1;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Conservative standard-global pricing and locally enforced request ceilings; never a quota claim.</summary>
public static class AnsweringReadinessEnvironment
{
    const string Prefix = "Cratis__Prompter__";

    /// <summary>Projects verified public prices and explicit workload choices, never keys or reset dates.</summary>
    /// <param name="args">Explicit activation and account choices.</param>
    /// <returns>Non-secret process configuration.</returns>
    /// <exception cref="InvalidAnsweringReadiness">The non-secret deployment settings are invalid.</exception>
    public static IEnumerable<EnvVarArgs> Create(AnsweringReadinessArgs args) =>
        Values(args).Select(pair => new EnvVarArgs { Name = pair.Key, Value = pair.Value });

    /// <summary>Gets the non-secret configuration projection for deployment and binding checks.</summary>
    /// <param name="args">Explicit workload choices.</param>
    /// <returns>Environment variable names and values.</returns>
    /// <exception cref="InvalidAnsweringReadiness">The non-secret deployment settings are invalid.</exception>
    public static IReadOnlyDictionary<string, string> Values(AnsweringReadinessArgs args)
    {
        AnsweringReadinessValidation.Validate(args);
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Protection__AnsweringEnabled"] = Flag(args.AnsweringEnabled),
            ["Protection__IndexingEnabled"] = Flag(args.IndexingEnabled),
            ["Protection__ConfiguredAllowedGuilds"] = string.Join(',', args.AllowedGuilds.Distinct(StringComparer.Ordinal)),
            ["Protection__DailyUsd"] = "5",
            ["Protection__Anthropic__DailyUsd"] = "5",
            ["Protection__Anthropic__Model"] = "claude-sonnet-5-5",
            ["Protection__Anthropic__InputUsdPerMillion"] = "4",
            ["Protection__Anthropic__OutputUsdPerMillion"] = "10",
            ["Protection__Anthropic__MaxInputTokens"] = "32000",
            ["Protection__Voyage__DailyUsd"] = "5",
            ["Protection__Voyage__Model"] = "voyage-4",
            ["Protection__Voyage__InputUsdPerMillion"] = "0.06",
            ["Protection__Voyage__OutputUsdPerMillion"] = "0",
            ["Protection__Voyage__MaxInputTokens"] = "32000",
            ["Anthropic__Model"] = "claude-sonnet-5-5",
            ["Anthropic__Answer__Model"] = "claude-sonnet-5-5",
            ["Anthropic__Answer__Effort"] = "high",
            ["Anthropic__Answer__MaxPromptTokens"] = "32000",
            ["Anthropic__Answer__MaxOutputTokens"] = "4096",
            ["Anthropic__Answer__PreferredAccount"] = args.AnswerAccount,
            ["Anthropic__Answer__AllowSecondary"] = Flag(args.AnswerAllowSecondary),
            ["Anthropic__IssueDraft__Model"] = "claude-sonnet-5-5",
            ["Anthropic__IssueDraft__Effort"] = "high",
            ["Anthropic__IssueDraft__MaxPromptTokens"] = "32000",
            ["Anthropic__IssueDraft__MaxOutputTokens"] = "4096",
            ["Anthropic__IssueDraft__PreferredAccount"] = args.IssueDraftAccount,
            ["Anthropic__IssueDraft__AllowSecondary"] = Flag(args.IssueDraftAllowSecondary),
            ["Answering__MaxOutputTokens"] = "4096",
            ["Answering__MinSemanticSimilarity"] = "0.5",
            ["Voyage__Model"] = "voyage-4",
            ["Voyage__Dimensions"] = "1024",
            ["Ingestion__PricePerMillionTokens"] = "0.06",
            ["Ingestion__MaxRunDollars"] = "5",
            ["Ingestion__MaxDailyDollars"] = "5"
        };
        foreach (var (guild, index) in args.AllowedGuilds.Distinct(StringComparer.Ordinal).Select((guild, index) => (guild, index)))
        {
            values[$"Protection__AllowedGuilds__{index}"] = guild;
        }

        return values.ToDictionary(pair => Prefix + pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    static string Flag(bool value) => value ? "true" : "false";
}
