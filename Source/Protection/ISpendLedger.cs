// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Atomic durable reservations under a shared global and provider daily ceiling.</summary>
public interface ISpendLedger
{
    /// <summary>Reserves exposure before dispatch, returning null on cap exhaustion.</summary>
    /// <param name="exposure">The bounded cost.</param>
    /// <param name="cancellationToken">The admission deadline.</param>
    /// <returns>A durable reservation identity, or null.</returns>
    Task<Guid?> Reserve(SpendExposure exposure, CancellationToken cancellationToken = default);

    /// <summary>Settles once; unknown dispatches must retain their reserved exposure.</summary>
    /// <param name="reservation">The reservation identity.</param>
    /// <param name="exposure">Known usage plus any still-unknown exposure.</param>
    /// <param name="cancellationToken">The settlement deadline.</param>
    /// <returns>The settlement task.</returns>
    Task Reconcile(Guid reservation, SpendExposure exposure, CancellationToken cancellationToken = default);
}
