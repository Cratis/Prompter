// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents one answered (or refused) question, recorded for quality follow-up and analytics.
/// Only a hash of the asking user is kept - see the GDPR decision record (D-8).
/// </summary>
/// <param name="UserHash">A one-way hash of the asking user's identifier.</param>
/// <param name="Source">Where the question came from, e.g. "discord-mention", "discord-ask", "cli".</param>
/// <param name="Question">The question as asked.</param>
/// <param name="Answer">The answer given.</param>
/// <param name="CitedPages">The pages cited in the answer.</param>
/// <param name="Confidence">The top passage score backing the answer.</param>
/// <param name="WasRefusal">Whether Prompter refused to answer.</param>
public record Interaction(
    string UserHash,
    string Source,
    string Question,
    string Answer,
    IEnumerable<PageUrl> CitedPages,
    double Confidence,
    bool WasRefusal);
