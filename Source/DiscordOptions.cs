// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Discord integration.
/// </summary>
public class DiscordOptions
{
    /// <summary>
    /// Gets or sets the bot token from the Discord developer portal.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether a bot token is configured. Validated at startup in bot mode only (the
    /// console <c>index</c>/<c>ask</c> modes never open a gateway), so an empty token fails fast at startup
    /// instead of surfacing later as an opaque Discord gateway authentication failure.
    /// </summary>
    public bool TokenIsPresent => Token.Length > 0;

    /// <summary>
    /// Gets or sets the identifier of the dedicated ask channel, where every plain message is treated as a
    /// question without requiring a mention. Every other channel still requires a mention.
    /// </summary>
    public ulong? AskChannelId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the help forum channel that gets automatic replies to new threads.
    /// </summary>
    public ulong? HelpForumChannelId { get; set; }

    /// <summary>
    /// Gets or sets the per-user rate-limiting options.
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of seconds a single answer may take before it is abandoned and the apology
    /// is sent instead, so a slow or stuck model never leaves a user waiting indefinitely.
    /// </summary>
    public int AnswerTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets a value indicating whether the answer timeout is usable: it must be strictly positive, because a
    /// value of zero (or less) makes the per-answer cancellation fire immediately, so every question is
    /// abandoned and answered with the apology. Validated at startup so the misconfiguration fails fast rather
    /// than silently turning every answer into an error reply.
    /// </summary>
    public bool AnswerTimeoutIsValid => AnswerTimeoutSeconds > 0;

    /// <summary>
    /// Gets or sets the friendly reply sent when a user exceeds their per-user question rate limit, in place
    /// of answering. Never a raw error and never silence.
    /// </summary>
    public string RateLimitedReply { get; set; } =
        "You've reached the question limit for now — give me a breather and try again in a few minutes.";

    /// <summary>
    /// Gets or sets the short apology sent when answering a question fails or times out, so the bot always
    /// says something rather than going silent.
    /// </summary>
    public string ErrorReply { get; set; } =
        "Something went wrong answering that on my end — sorry. Please try again in a moment.";
}
