// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Reserves nonqueued running and rolling-minute capacity atomically.</summary>
/// <param name="concurrency">Maximum simultaneous turns.</param>
/// <param name="perMinute">Maximum accepted turns in a rolling minute; zero denies.</param>
/// <param name="clock">The expiry clock.</param>
public sealed class BoundedWork(int concurrency, int perMinute, TimeProvider clock)
{
    readonly Lock _gate = new();
    readonly Queue<DateTimeOffset> _accepted = new();
    int _running;

    /// <summary>Reserves both capacities, without waiting or retaining a rejected request.</summary>
    /// <returns>A disposable reservation, or null if either capacity is exhausted.</returns>
    public WorkTurn? TryEnter()
    {
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            while (_accepted.TryPeek(out var accepted) && accepted <= now - TimeSpan.FromMinutes(1))
            {
                _accepted.Dequeue();
            }
            if (_running >= concurrency || _accepted.Count >= perMinute)
            {
                return null;
            }
            _accepted.Enqueue(now);
            _running++;
            return new(Release);
        }
    }

    void Release()
    {
        lock (_gate)
        {
            _running--;
        }
    }
}
