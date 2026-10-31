// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Digest;

/// <summary>
/// Computes UTC weekly slots without a persisted delivery marker.
/// </summary>
public static class DigestSchedule
{
    /// <summary>
    /// Finds the next slot strictly after now. Hours outside 0 through 23 are clamped.
    /// </summary>
    /// <param name="now">The current instant.</param>
    /// <param name="day">The scheduled weekday.</param>
    /// <param name="hourUtc">The scheduled UTC hour.</param>
    /// <returns>The next UTC delivery instant.</returns>
    public static DateTimeOffset NextSlot(DateTimeOffset now, DayOfWeek day, int hourUtc)
    {
        var utc = now.ToUniversalTime();
        var days = ((int)day - (int)utc.DayOfWeek + 7) % 7;
        var slot = new DateTimeOffset(utc.Date, TimeSpan.Zero).AddDays(days).AddHours(Math.Clamp(hourUtc, 0, 23));
        return slot > utc ? slot : slot.AddDays(7);
    }

    /// <summary>
    /// Finds the half-open seven-day interval preceding a delivery slot.
    /// </summary>
    /// <param name="slot">The delivery instant.</param>
    /// <returns>The inclusive start and exclusive end, in UTC.</returns>
    public static (DateTimeOffset From, DateTimeOffset To) WindowFor(DateTimeOffset slot) => (slot.ToUniversalTime().AddDays(-7), slot.ToUniversalTime());
}
