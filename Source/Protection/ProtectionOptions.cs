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

    /// <summary>Gets or sets the only users permitted to use the bot. Empty permits everyone not denied.</summary>
    public IReadOnlyList<ulong> AllowedUsers { get; set; } = [];

    /// <summary>Gets or sets users refused silently. Denial wins over the allow list.</summary>
    public IReadOnlyList<ulong> DeniedUsers { get; set; } = [];

    /// <summary>Gets or sets simultaneous interactive paid operations, without a waiting queue.</summary>
    public int Concurrency { get; set; } = 3;

    /// <summary>Gets or sets the global accepted-request allowance per hour.</summary>
    public int GlobalPerHour { get; set; } = 60;

    /// <summary>Gets or sets admissions per requester per UTC day (0–100); zero denies.</summary>
    public int UserAdmissionsPerDay { get; set; } = 30;

    /// <summary>Gets or sets minimum seconds between requester admissions (1–86400).</summary>
    public int UserAdmissionCooldownSeconds { get; set; } = 15;

    /// <summary>Gets or sets requester-attributable refusals inside the strike window that start an escalated pause (0–100); zero disables escalation.</summary>
    public int RefusalStrikes { get; set; } = 5;

    /// <summary>Gets or sets the window in seconds in which refusals count as strikes (1–86400).</summary>
    public int RefusalStrikeWindowSeconds { get; set; } = 600;

    /// <summary>Gets or sets the first escalated pause in seconds (1–86400); each repeat doubles it.</summary>
    public int EscalatedCooldownSeconds { get; set; } = 300;

    /// <summary>Gets or sets the longest escalated pause in seconds (at least the first pause, at most 604800).</summary>
    public int MaxEscalatedCooldownSeconds { get; set; } = 21600;

    /// <summary>Gets or sets the channel/thread accepted-request allowance per hour.</summary>
    public int ChannelPerHour { get; set; } = 20;

    /// <summary>Gets or sets the maximum current question length.</summary>
    public int MaxQuestionCharacters { get; set; } = 4000;

    /// <summary>Gets or sets the duplicate suppression interval.</summary>
    public int DuplicateSeconds { get; set; } = 120;

    /// <summary>Gets or sets the per-user refusal reply interval.</summary>
    public int UserRefusalSeconds { get; set; } = 600;

    /// <summary>Gets or sets simultaneous nonpaid actions without a waiting queue (1–8).</summary>
    public int ActionConcurrency { get; set; } = 2;

    /// <summary>Gets or sets accepted actions per rolling minute (0–120); zero denies.</summary>
    public int ActionPerMinute { get; set; } = 12;

    /// <summary>Gets or sets accepted actions per channel per rolling minute (0–120); zero denies.</summary>
    public int ActionChannelPerMinute { get; set; } = 6;

    /// <summary>Gets or sets accepted action clicks per user per window (0–100); zero denies.</summary>
    public int ActionClicksPerUser { get; set; } = 3;

    /// <summary>Gets or sets the action user's window in seconds (1–86400).</summary>
    public int ActionWindowSeconds { get; set; } = 600;

    /// <summary>Gets or sets simultaneous feedback lifecycles without a waiting queue (1–8).</summary>
    public int FeedbackConcurrency { get; set; } = 2;

    /// <summary>Gets or sets feedback clicks per rolling minute (0–120); zero denies.</summary>
    public int FeedbackPerMinute { get; set; } = 30;

    /// <summary>Gets or sets the shared feedback lifecycle deadline in milliseconds (100–2500).</summary>
    public int FeedbackWriteTimeoutMilliseconds { get; set; } = 2000;

    /// <summary>Gets or sets the feedback button clicks one user may make per window.</summary>
    public int FeedbackClicksPerUser { get; set; } = 10;

    /// <summary>Gets or sets the feedback click window, and how long the latest verdict per answer is remembered.</summary>
    public int FeedbackWindowSeconds { get; set; } = 600;

    /// <summary>Gets or sets the per-channel refusal reply interval.</summary>
    public int ChannelRefusalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the maximum number of transient entries per cache; exhaustion denies admission.</summary>
    public int LocalCapacity { get; set; } = 10000;

    /// <summary>Gets or sets the shared UTC-day dollar ceiling for all paid work.</summary>
    public decimal DailyUsd { get; set; } = 5;

    /// <summary>Gets or sets verified conservative Anthropic prices and limits.</summary>
    public ProviderBudget Anthropic { get; set; } = new();

    /// <summary>
    /// Gets or sets the verified Anthropic prices and limits for answer generation. The default prices the
    /// default answer model, Haiku 5.5, at its published standard rates for prompts up to 100,000 tokens. The
    /// input price is the highest applicable one (the 5-minute cache-write rate, 1.25 times the base input price).
    /// </summary>
    public ProviderBudget AnthropicAnswer { get; set; } = new()
    {
        Model = "claude-haiku-5-5",
        InputUsdPerMillion = 0.25m,
        CacheWriteUsdPerMillion = 0.25m,
        CacheReadUsdPerMillion = 0.02m,
        OutputUsdPerMillion = 0.50m,
        MaxInputTokens = 32000
    };

    /// <summary>Gets or sets verified conservative Voyage prices and limits.</summary>
    public ProviderBudget Voyage { get; set; } = new();

    /// <summary>Gets or sets simultaneous authenticated webhook lifecycles (1–8), without a queue.</summary>
    public int WebhookConcurrency { get; set; } = 2;

    /// <summary>Gets or sets authenticated webhook starts per minute (0–120); zero denies.</summary>
    public int WebhookPerMinute { get; set; } = 12;

    /// <summary>Gets or sets maintainer notifications per minute (0–120); zero denies.</summary>
    public int WebhookNotificationsPerMinute { get; set; } = 6;

    /// <summary>Gets or sets the maximum raw webhook body size, including chunked bodies.</summary>
    public int WebhookBytes { get; set; } = 65536;

    /// <summary>Gets a value indicating whether independent action bounds are usable.</summary>
    public bool ActionLimitsAreValid => ActionConcurrency is >= 1 and <= 8 && ActionPerMinute is >= 0 and <= 120 &&
        ActionChannelPerMinute is >= 0 and <= 120 && ActionClicksPerUser is >= 0 and <= 100 && ActionWindowSeconds is >= 1 and <= 86400;

    /// <summary>Gets a value indicating whether independent feedback bounds are usable.</summary>
    public bool FeedbackLimitsAreValid => FeedbackConcurrency is >= 1 and <= 8 && FeedbackPerMinute is >= 0 and <= 120 &&
        FeedbackWriteTimeoutMilliseconds is >= 100 and <= 2500;

    /// <summary>Gets a value indicating whether requester fairness bounds are usable.</summary>
    public bool RequesterLimitsAreValid => UserAdmissionsPerDay is >= 0 and <= 100 && UserAdmissionCooldownSeconds is >= 1 and <= 86400 &&
        RefusalStrikes is >= 0 and <= 100 && RefusalStrikeWindowSeconds is >= 1 and <= 86400 && EscalatedCooldownSeconds is >= 1 and <= 86400 &&
        MaxEscalatedCooldownSeconds >= EscalatedCooldownSeconds && MaxEscalatedCooldownSeconds <= 604800;

    /// <summary>Gets a value indicating whether the user lists contain only usable identifiers.</summary>
    public bool UserListsAreValid => !AllowedUsers.Contains(0UL) && !DeniedUsers.Contains(0UL);

    /// <summary>Gets a value indicating whether webhook resource bounds are usable.</summary>
    public bool WebhookLimitsAreValid => WebhookConcurrency is >= 1 and <= 8 && WebhookPerMinute is >= 0 and <= 120 &&
        WebhookNotificationsPerMinute is >= 0 and <= 120;

    /// <summary>Gets a value indicating whether local bounds are usable.</summary>
    public bool IsValid => Concurrency > 0 && GlobalPerHour >= 0 && ChannelPerHour >= 0 &&
        MaxQuestionCharacters > 0 && DuplicateSeconds > 0 && UserRefusalSeconds > 0 &&
        ChannelRefusalSeconds > 0 && ActionLimitsAreValid && FeedbackLimitsAreValid && RequesterLimitsAreValid && UserListsAreValid && WebhookLimitsAreValid && FeedbackClicksPerUser > 0 && FeedbackWindowSeconds > 0 && LocalCapacity > 0 && DailyUsd >= 0 && WebhookBytes > 0;
}
