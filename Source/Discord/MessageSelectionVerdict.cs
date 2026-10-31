// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>The result of judging a message selection.</summary>
public enum MessageSelectionVerdict
{
    /// <summary>The plain text can be answered.</summary>
    Accept = 0,

    /// <summary>There is no plain text to answer.</summary>
    Empty = 1,

    /// <summary>The text exceeds the question ceiling.</summary>
    TooLong = 2,

    /// <summary>A bot authored the message.</summary>
    FromBot = 3,

    /// <summary>Discord generated the message.</summary>
    System = 4
}
