// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// Holds one answer's feedback lifecycle (check, acknowledge, write, commit) so concurrent clicks on the same
/// answer run one at a time, in arrival order. Disposing releases the answer to the next click.
/// </summary>
/// <param name="release">The action that releases the answer.</param>
public sealed class FeedbackTurn(Action release) : IDisposable
{
    int _released;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _released, 1) == 0)
        {
            release();
        }
    }
}
