// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>The transport callbacks covered by one nonpaid action reservation.</summary>
/// <param name="Defer">Acknowledges an admitted action before any lookup.</param>
/// <param name="Work">Checks ownership and performs the action.</param>
/// <param name="Refuse">Sends an immediate, suppressed refusal.</param>
/// <param name="Followup">Displays the existing action result.</param>
public sealed record NonpaidCallbacks(
    Func<CancellationToken, Task> Defer,
    Func<CancellationToken, Task<string>> Work,
    Func<string, CancellationToken, Task> Refuse,
    Func<string, CancellationToken, Task> Followup);
