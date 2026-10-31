// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Telemetry;

/// <summary>Content-free measurements from a completed attempt.</summary>
/// <param name="Usage">Known provider counts, if supplied.</param>
/// <param name="Outcome">The bounded result category.</param>
/// <param name="Duration">Elapsed attempt time.</param>
public sealed record ProviderResult(UsageDetails? Usage, string Outcome, TimeSpan Duration);
