// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The verdict a user gives on an answer through the feedback buttons.
/// </summary>
public enum FeedbackVerdict
{
    /// <summary>
    /// The answer was helpful (👍).
    /// </summary>
    Up = 0,

    /// <summary>
    /// The answer was not helpful (👎).
    /// </summary>
    Down = 1
}
