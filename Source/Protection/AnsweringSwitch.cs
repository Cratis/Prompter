// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Protection;

/// <summary>
/// The in-memory kill switch every entry point reads. It is closed until the persisted flag has been read once,
/// and it is paused when the persisted flag, or a local override whose write failed, says so.
/// </summary>
/// <param name="logger">The logger; transitions are logged without any identity.</param>
public sealed class AnsweringSwitch(ILogger<AnsweringSwitch> logger)
{
    readonly Lock _lock = new();
    volatile bool _paused = true;
    bool _loaded;
    bool _persisted;
    bool _override;
    long _version;

    /// <summary>Gets a value indicating whether answering is paused, including before the first successful read.</summary>
    public bool Paused => _paused;

    /// <summary>Gets a value indicating whether the persisted flag has not been read successfully yet.</summary>
    public bool AwaitingFirstRead
    {
        get
        {
            lock (_lock)
            {
                return !_loaded;
            }
        }
    }

    /// <summary>Gets a value indicating whether a pause is held locally because it could not be persisted.</summary>
    public bool LocalOverride
    {
        get
        {
            lock (_lock)
            {
                return _override;
            }
        }
    }

    /// <summary>Gets the sequence number a refresh must capture before it starts reading.</summary>
    public long Version
    {
        get
        {
            lock (_lock)
            {
                return _version;
            }
        }
    }

    /// <summary>Applies a successful read of the persisted flag, unless a command changed the switch after the read began.</summary>
    /// <param name="persisted">The persisted value that was read.</param>
    /// <param name="startedAt">The <see cref="Version"/> captured before the read started.</param>
    /// <returns><see langword="true"/> when applied; <see langword="false"/> when discarded as stale.</returns>
    public bool Refresh(bool persisted, long startedAt)
    {
        lock (_lock)
        {
            if (startedAt != _version)
            {
                return false;
            }

            _loaded = true;
            _persisted = persisted;
            Publish();
            return true;
        }
    }

    /// <summary>Pauses locally before the flag is written, so the pause takes effect immediately.</summary>
    public void BeginPause()
    {
        lock (_lock)
        {
            _version++;
            _override = true;
            Publish();
        }
    }

    /// <summary>Records that the pause was persisted, so the local override is no longer needed.</summary>
    public void PauseStored()
    {
        lock (_lock)
        {
            _version++;
            _loaded = true;
            _persisted = true;
            _override = false;
            Publish();
        }
    }

    /// <summary>Records a successful, authorized resume that was persisted. This is the only way to clear a local override.</summary>
    public void Resumed()
    {
        lock (_lock)
        {
            _version++;
            _loaded = true;
            _persisted = false;
            _override = false;
            Publish();
        }
    }

    void Publish()
    {
        var paused = !_loaded || _persisted || _override;
        if (paused == _paused)
        {
            return;
        }

        _paused = paused;
        if (paused)
        {
            logger.AnsweringPaused();
        }
        else
        {
            logger.AnsweringResumed();
        }

        ProviderMetrics.Event("protection", paused ? "paused" : "resumed");
    }
}
