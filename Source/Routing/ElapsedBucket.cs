// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Bounded elapsed-time metadata.</summary>
public enum ElapsedBucket
{
    /// <summary>Less than 100 milliseconds.</summary>
    Under100Milliseconds = 0,

    /// <summary>Less than 500 milliseconds.</summary>
    Under500Milliseconds = 1,

    /// <summary>Less than one second.</summary>
    UnderOneSecond = 2,

    /// <summary>One second or more.</summary>
    Deadline = 3
}
