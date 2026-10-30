// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Configurable admission and cost boundaries. Unknown provider prices fail closed.</summary>
public class ProtectionOptions
{
    /// <summary>Gets or sets whether answers and issue drafting are enabled.</summary>
    public bool AnsweringEnabled { get; set; } = true;

    /// <summary>Gets or sets whether indexing is enabled.</summary>
    public bool IndexingEnabled { get; set; } = true;

    /// <summary>Gets or sets whether GitHub writes are enabled.</summary>
    public bool GitHubWritesEnabled { get; set; } = true;

    /// <summary>Gets or sets the guilds permitted to use the bot. Empty denies all.</summary>
    public IReadOnlyList<ulong> AllowedGuilds { get; set; } = [];

    /// <summary>Gets or sets channels denied even within an allowed guild.</summary>
    public IReadOnlyList<ulong> DeniedChannels { get; set; } = [];

    /// <summary>Gets or sets simultaneous interactive paid operations, without a waiting queue.</summary>
    public int Concurrency { get; set; } = 3;

    /// <summary>Gets or sets the global accepted-request allowance per hour.</summary>
    public int GlobalPerHour { get; set; } = 60;

    /// <summary>Gets or sets the channel/thread accepted-request allowance per hour.</summary>
    public int ChannelPerHour { get; set; } = 20;

    /// <summary>Gets or sets the maximum current question length.</summary>
    public int MaxQuestionCharacters { get; set; } = 4000;

    /// <summary>Gets or sets the duplicate suppression interval.</summary>
    public int DuplicateSeconds { get; set; } = 120;

    /// <summary>Gets or sets the per-user refusal reply interval.</summary>
    public int UserRefusalSeconds { get; set; } = 600;

    /// <summary>Gets or sets the per-channel refusal reply interval.</summary>
    public int ChannelRefusalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the maximum number of transient entries per cache; exhaustion denies admission.</summary>
    public int LocalCapacity { get; set; } = 10000;

    /// <summary>Gets or sets the shared UTC-day dollar ceiling for all paid work.</summary>
    public decimal DailyUsd { get; set; } = 5;

    /// <summary>Gets or sets verified conservative Anthropic prices and limits.</summary>
    public ProviderBudget Anthropic { get; set; } = new();

    /// <summary>Gets or sets verified conservative Voyage prices and limits.</summary>
    public ProviderBudget Voyage { get; set; } = new();

    /// <summary>Gets or sets the maximum raw webhook body size, including chunked bodies.</summary>
    public int WebhookBytes { get; set; } = 65536;

    /// <summary>Gets a value indicating whether local bounds are usable.</summary>
    public bool IsValid => Concurrency > 0 && GlobalPerHour >= 0 && ChannelPerHour >= 0 &&
        MaxQuestionCharacters > 0 && DuplicateSeconds > 0 && UserRefusalSeconds > 0 &&
        ChannelRefusalSeconds > 0 && LocalCapacity > 0 && DailyUsd >= 0 && WebhookBytes > 0;
}
