// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>A running-work reservation whose throughput consumption is never refunded.</summary>
/// <param name="release">Releases running capacity.</param>
public sealed class WorkTurn(Action release) : IDisposable
{
    Action? _release = release;

    /// <inheritdoc/>
    public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
}
