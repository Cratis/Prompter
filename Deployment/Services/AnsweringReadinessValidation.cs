// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Rejects invalid non-secret deployment settings before creating workload resources.</summary>
public static class AnsweringReadinessValidation
{
    /// <summary>The sole approved production guild, represented without numeric precision loss.</summary>
    public const string ApprovedGuild = "1182595891576717413";

    static readonly HashSet<string> _accounts = new(StringComparer.Ordinal) { "primary", "secondary" };

    /// <summary>Validates explicit account vocabulary, guild authorization and secondary opt-in.</summary>
    /// <param name="args">The workload configuration.</param>
    /// <exception cref="InvalidAnsweringReadiness">An account or guild setting is not authorized.</exception>
    public static void Validate(AnsweringReadinessArgs args)
    {
        Require(_accounts.Contains(args.AnswerAccount), "Answer account must be primary or secondary.");
        Require(_accounts.Contains(args.IssueDraftAccount), "Issue-draft account must be primary or secondary.");
        Require(args.AllowedGuilds.All(guild => guild == ApprovedGuild), "Only the approved production guild may be configured.");
        if (!args.AnsweringEnabled)
        {
            return;
        }

        Require(args.AllowedGuilds.Count > 0, "Enabled answering requires an explicit approved guild.");
        Require(args.AnswerAccount != "secondary" || args.AnswerAllowSecondary, "Secondary answer selection requires task opt-in.");
        Require(args.IssueDraftAccount != "secondary" || args.IssueDraftAllowSecondary, "Secondary issue-draft selection requires task opt-in.");
    }

    static void Require(bool condition, string reason)
    {
        if (!condition)
        {
            throw new InvalidAnsweringReadiness(reason);
        }
    }
}
