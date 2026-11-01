// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Retry-inclusive exposure and admission limits for the current UTC day.</summary>
/// <param name="Exposure">The conservative token and dollar exposure.</param>
/// <param name="DailyLimit">The configured daily dollar ceiling.</param>
/// <param name="Scheduled">Whether this reserves the single scheduled slot.</param>
/// <param name="Full">Whether this reserves a confirmed full run; admitted full reservations record the day's full-run ceiling.</param>
public record IndexReservation(IndexExposure Exposure, decimal DailyLimit, bool Scheduled, bool Full);
