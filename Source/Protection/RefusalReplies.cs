// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Protection;

/// <summary>Fixed refusal texts that name the limit that applied and how long to wait.</summary>
public static class RefusalReplies
{
    /// <summary>Builds the refusal text for a limit.</summary>
    /// <param name="limit">The limit that refused the request.</param>
    /// <param name="wait">How long until the request would be admitted.</param>
    /// <param name="dailyAllowance">The configured allowance per UTC day.</param>
    /// <returns>The text, or <see langword="null"/> when the generic reply should be used.</returns>
    public static string? For(AdmissionLimit limit, TimeSpan wait, int dailyAllowance) => limit switch
    {
        AdmissionLimit.Cooldown => $"Give me a moment between questions — try again in {Approximately(wait)}.",
        AdmissionLimit.Daily => $"You've used today's {dailyAllowance.ToString(CultureInfo.InvariantCulture)} questions. Your allowance resets at 00:00 UTC (in {Approximately(wait)}).",
        AdmissionLimit.Burst => $"That's a lot of questions in a short time — try again in {Approximately(wait)}.",
        AdmissionLimit.Duplicate => $"I just got that same question from you — ask again in {Approximately(wait)} if you still need it.",
        AdmissionLimit.Escalated => $"You've hit the limits several times, so I'm pausing your questions for {Approximately(wait)}.",
        AdmissionLimit.Busy => $"I'm answering a lot of questions here right now — try again in {Approximately(wait)}.",
        _ => null
    };

    static string Approximately(TimeSpan wait)
    {
        var seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
        if (seconds <= 90)
        {
            return $"about {Unit(seconds, "second")}";
        }

        var minutes = (int)Math.Ceiling(wait.TotalMinutes);
        return minutes <= 90 ? $"about {Unit(minutes, "minute")}" : $"about {Unit((int)Math.Ceiling(wait.TotalHours), "hour")}";
    }

    static string Unit(int count, string name) => $"{count.ToString(CultureInfo.InvariantCulture)} {name}{(count == 1 ? string.Empty : "s")}";
}
