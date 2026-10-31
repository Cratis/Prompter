// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Pulumi;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Non-secret, explicit workload activation and account choices. Defaults activate nothing.</summary>
public sealed record AnsweringReadinessArgs
{
    /// <summary>Gets whether answering is explicitly enabled.</summary>
    public bool AnsweringEnabled { get; init; }

    /// <summary>Gets whether indexing is explicitly enabled.</summary>
    public bool IndexingEnabled { get; init; }

    /// <summary>Gets the explicitly approved guild list. Empty permits no guilds.</summary>
    public IReadOnlyList<string> AllowedGuilds { get; init; } = [];

    /// <summary>Gets the answer account, using a closed runtime vocabulary.</summary>
    public string AnswerAccount { get; init; } = "primary";

    /// <summary>Gets the issue-draft account, using a closed runtime vocabulary.</summary>
    public string IssueDraftAccount { get; init; } = "primary";

    /// <summary>Gets the independent answer opt-in to secondary use.</summary>
    public bool AnswerAllowSecondary { get; init; }

    /// <summary>Gets the independent issue-draft opt-in to secondary use.</summary>
    public bool IssueDraftAllowSecondary { get; init; }

    /// <summary>Reads non-secret deployment configuration without inspecting provider allocation state.</summary>
    /// <param name="config">The deployment configuration.</param>
    /// <returns>Explicit workload choices with disabled defaults.</returns>
    /// <exception cref="InvalidAnsweringReadiness">The non-secret deployment settings are invalid.</exception>
    public static AnsweringReadinessArgs From(Config config)
    {
        var args = new AnsweringReadinessArgs
        {
            AnsweringEnabled = config.GetBoolean("answeringEnabled") ?? false,
            IndexingEnabled = config.GetBoolean("indexingEnabled") ?? false,
            AllowedGuilds = config.GetObject<string[]>("allowedGuilds") ?? [],
            AnswerAccount = config.Get("answerPreferredAccount") ?? "primary",
            IssueDraftAccount = config.Get("issueDraftPreferredAccount") ?? "primary",
            AnswerAllowSecondary = config.GetBoolean("answerAllowSecondary") ?? false,
            IssueDraftAllowSecondary = config.GetBoolean("issueDraftAllowSecondary") ?? false
        };
        AnsweringReadinessValidation.Validate(args);
        return args;
    }
}
