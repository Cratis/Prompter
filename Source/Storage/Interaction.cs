// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents one answered (or refused) question, recorded as anonymous operational signal for quality
/// follow-up. Deliberately holds no personal data - no message content and no user identifier - so the log
/// stays outside GDPR scope entirely (data minimization, D-13 amending D-8).
/// </summary>
/// <param name="Source">Where the question came from, e.g. "discord-mention", "discord-ask", "cli".</param>
/// <param name="CitedPages">The pages cited in the answer.</param>
/// <param name="Confidence">The top passage score backing the answer.</param>
/// <param name="WasRefusal">Whether Prompter refused to answer.</param>
public record Interaction(
    string Source,
    IEnumerable<PageUrl> CitedPages,
    double Confidence,
    bool WasRefusal);
