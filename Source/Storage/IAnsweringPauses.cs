// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>Persists the maintainer kill switch that pauses answering without a redeploy.</summary>
public interface IAnsweringPauses
{
    /// <summary>Reads the persisted pause flag.</summary>
    /// <param name="cancellationToken">The read deadline.</param>
    /// <returns>Whether answering is paused; a missing row means not paused.</returns>
    Task<bool> IsPaused(CancellationToken cancellationToken);

    /// <summary>Persists the pause flag.</summary>
    /// <param name="paused">Whether answering is paused.</param>
    /// <param name="cancellationToken">The write deadline.</param>
    /// <returns>A task that completes when the flag is stored.</returns>
    Task Set(bool paused, CancellationToken cancellationToken);
}
