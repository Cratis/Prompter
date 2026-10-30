// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Uncalibrated confidence, not measured accuracy.</summary>
public enum ConfidenceBucket
{
    /// <summary>No distribution available.</summary>
    Unknown = 0,

    /// <summary>Below configured thresholds.</summary>
    Low = 1,

    /// <summary>Meets configured thresholds, not calibrated accuracy.</summary>
    High = 2
}
