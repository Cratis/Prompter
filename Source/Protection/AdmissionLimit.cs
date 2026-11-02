// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>The limit that refused an admission.</summary>
public enum AdmissionLimit
{
    /// <summary>The minimum gap between a requester's questions.</summary>
    Cooldown = 0,

    /// <summary>The requester's allowance for the UTC day.</summary>
    Daily = 1,

    /// <summary>The requester's burst allowance.</summary>
    Burst = 2,

    /// <summary>The same question was just asked.</summary>
    Duplicate = 3,

    /// <summary>A temporary cool-down after repeated refusals.</summary>
    Escalated = 4,

    /// <summary>The global or channel allowance is spent.</summary>
    Busy = 5,

    /// <summary>The cause is unknown, such as full local memory.</summary>
    Unknown = 6
}
