// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Advisory intent, never permission to act.</summary>
public enum ClassificationIntent
{
    /// <summary>Seeking help.</summary>
    Question = 0,

    /// <summary>Reporting unexpected behavior.</summary>
    Bug = 1,

    /// <summary>Requesting a capability.</summary>
    Feature = 2,

    /// <summary>Suggesting an improvement.</summary>
    Idea = 3,

    /// <summary>Discussing documentation.</summary>
    Documentation = 4
}
