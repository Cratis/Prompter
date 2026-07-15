// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Scores whether the bot's refusal decision matched the golden set - the guard against the over-eager-answer
/// failure mode, where a confident answer to an out-of-scope question is the worst possible outcome.
/// </summary>
public static class RefusalAccuracy
{
    /// <summary>
    /// Determines whether the bot's refusal decision was correct. An out-of-scope question must be refused;
    /// an in-scope question must be answered. A mismatch either way is a failure.
    /// </summary>
    /// <param name="expectedRefusal">Whether the golden set expects a refusal (the question is out of scope).</param>
    /// <param name="actualRefusal">Whether the bot actually refused.</param>
    /// <returns>
    /// <see langword="true"/> when the decision matched what was expected; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsCorrect(bool expectedRefusal, bool actualRefusal) => expectedRefusal == actualRefusal;
}
