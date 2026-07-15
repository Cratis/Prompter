// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Maps a <see cref="FeedbackVerdict"/> to and from its canonical text form. The same short tokens are
/// stored in the <c>interactions.feedback</c> column and carried in a feedback button's custom id, so both
/// the storage layer and the Discord layer agree on the vocabulary from this single place.
/// </summary>
public static class FeedbackVerdicts
{
    const string UpText = "up";
    const string DownText = "down";

    /// <summary>
    /// Gets the canonical text form of a verdict.
    /// </summary>
    /// <param name="verdict">The verdict to render.</param>
    /// <returns>The text token, <c>"up"</c> or <c>"down"</c>.</returns>
    public static string ToText(this FeedbackVerdict verdict) => verdict == FeedbackVerdict.Up ? UpText : DownText;

    /// <summary>
    /// Parses a verdict from its canonical text form.
    /// </summary>
    /// <param name="text">The text token to parse.</param>
    /// <param name="verdict">The parsed verdict when the token is recognized.</param>
    /// <returns><see langword="true"/> when <paramref name="text"/> is a recognized token; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string? text, out FeedbackVerdict verdict)
    {
        switch (text)
        {
            case UpText:
                verdict = FeedbackVerdict.Up;
                return true;
            case DownText:
                verdict = FeedbackVerdict.Down;
                return true;
            default:
                verdict = default;
                return false;
        }
    }
}
