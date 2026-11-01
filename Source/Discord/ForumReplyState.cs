// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// What the recent messages of a forum post say about whether it still needs an answer.
/// </summary>
public enum ForumReplyState
{
    /// <summary>Nobody but the author has replied and Prompter has not posted.</summary>
    Unanswered = 0,

    /// <summary>Prompter already posted in the thread.</summary>
    PrompterAnswered = 1,

    /// <summary>A human other than the author replied.</summary>
    HumanReplied = 2,

    /// <summary>The thread has too many messages to judge, so it is left alone.</summary>
    Busy = 3
}
