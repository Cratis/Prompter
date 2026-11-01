// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// What to do with a feedback click once it is compared with the latest verdict seen for its answer.
/// </summary>
public enum FeedbackVote
{
    /// <summary>The verdict is new or changed: acknowledge it and write it.</summary>
    Record = 0,

    /// <summary>The same verdict is already the latest: send nothing and write nothing.</summary>
    Repeat = 1,

    /// <summary>Local memory is full: fail closed without writing.</summary>
    Refused = 2
}
