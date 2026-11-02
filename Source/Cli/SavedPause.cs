// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Cli;

/// <summary>
/// Reads the saved maintainer pause flag once for the terminal modes, which run no background refresh.
/// The switch stays closed unless the read succeeds and says answering is not paused.
/// </summary>
public static class SavedPause
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    /// <summary>Reads the saved flag and opens the switch when answering is not paused.</summary>
    /// <param name="answering">The switch the paid work reads.</param>
    /// <param name="pauses">The persisted flag.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A message explaining why answering stays closed, or <see langword="null"/> when it may proceed.</returns>
    public static async Task<string?> Load(AnsweringSwitch answering, IAnsweringPauses pauses, CancellationToken cancellationToken = default)
    {
        bool paused;
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(_timeout);
            var started = answering.Version;
            paused = await pauses.IsPaused(linked.Token);
            answering.Refresh(paused, started);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return $"Could not read the saved answering pause flag ({exception.GetType().Name}); answering stays closed.";
        }

        return paused ? "Answering is paused by a maintainer (/answering pause); not running. Resume it in Discord first." : null;
    }
}
