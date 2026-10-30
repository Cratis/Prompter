// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Telemetry;

/// <summary>
/// Integration seam for shared durable reservations and independent provider/account/task accounting.
/// Implementations must reserve before each dispatch under the same logical-operation/global budget,
/// reconcile known use and retain exposure for null (unknown) usage, including failed calls.
/// No prompt, response, key, organization identifier or provider error body crosses this boundary.
/// </summary>
public interface IProviderAccounting
{
    /// <summary>
    /// Atomically reserves a conservative upper bound or refuses dispatch. Must fail closed on storage failure.
    /// </summary>
    /// <param name="dispatch">Bounded dispatch metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Whether the reservation was acquired.</returns>
    Task<bool> Reserve(ProviderDispatch dispatch, CancellationToken cancellationToken);

    /// <summary>
    /// Reconciles a dispatched reservation. Cancellation must not release unknown charge exposure.
    /// </summary>
    /// <param name="dispatch">The dispatch metadata.</param>
    /// <param name="usage">Measured usage, or null for unknown exposure.</param>
    /// <param name="finishReason">Sanitized finish category.</param>
    /// <returns>The completion task.</returns>
    Task Complete(ProviderDispatch dispatch, UsageDetails? usage, string finishReason);
}
