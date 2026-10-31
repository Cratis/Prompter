// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Protection;

/// <summary>Transactional attempt funding over the shared spend ledger.</summary>
public interface IProviderAttemptStore
{
    /// <summary>Claims prepaid exposure or independently funds a new attempt, exactly once.</summary>
    /// <param name="dispatch">Validated bounded metadata.</param>
    /// <param name="parent">The prepaid parent, or null.</param>
    /// <param name="upper">Conservative exposure.</param>
    /// <param name="cancellationToken">Reservation deadline.</param>
    /// <returns>Whether this caller obtained a new permit.</returns>
    Task<bool> Reserve(ProviderDispatch dispatch, Guid? parent, decimal upper, CancellationToken cancellationToken);

    /// <summary>Durably records completion and settles independent funding at most once.</summary>
    /// <param name="dispatch">Exact reserved metadata.</param>
    /// <param name="cost">Fully validated cost, or null for unknown usage.</param>
    /// <param name="cancellationToken">Independent cleanup deadline.</param>
    /// <returns>Whether completion is durable.</returns>
    Task<bool> Complete(ProviderDispatch dispatch, decimal? cost, CancellationToken cancellationToken);
}
