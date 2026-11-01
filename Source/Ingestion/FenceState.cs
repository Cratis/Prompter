// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Tracks whether a fenced code block (backtick or tilde) is open, so the state survives across the paragraph parts
/// of a section.
/// </summary>
internal sealed class FenceState
{
    const int MinimumFenceLength = 3;

    char _marker;
    int _length;

    /// <summary>Gets a value indicating whether a fenced code block is currently open.</summary>
    public bool Open => _marker != '\0';

    /// <summary>
    /// Updates the state from one line: an opening fence opens a block, and a closing fence of the same character
    /// and at least the same length closes it.
    /// </summary>
    /// <param name="line">The line to observe.</param>
    public void Observe(string line)
    {
        var trimmed = line.TrimStart();
        if (!StartsFence(trimmed))
        {
            return;
        }

        var run = 1;
        while (run < trimmed.Length && trimmed[run] == trimmed[0])
        {
            run++;
        }

        if (run < MinimumFenceLength)
        {
            return;
        }

        if (Open)
        {
            if (ClosesFence(trimmed, run))
            {
                _marker = '\0';
                _length = 0;
            }

            return;
        }

        if (InvalidBacktickInfo(trimmed, run))
        {
            return;
        }

        _marker = trimmed[0];
        _length = run;
    }

    static bool StartsFence(string line) =>
        line.Length >= MinimumFenceLength && (line[0] == '`' || line[0] == '~');

    static bool InvalidBacktickInfo(string line, int run) => line[0] == '`' && line.IndexOf('`', run) >= 0;

    bool ClosesFence(string line, int run) =>
        line[0] == _marker && run >= _length && string.IsNullOrWhiteSpace(line[run..]);
}
