// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Bounded reasons, without provider error text.</summary>
public enum ClassificationFallback
{
    /// <summary>No fallback needed.</summary>
    None = 0,

    /// <summary>No endpoint configured.</summary>
    NotConfigured = 1,

    /// <summary>Endpoint is not explicitly allowed and private.</summary>
    EndpointRejected = 2,

    /// <summary>Response failed contract validation.</summary>
    InvalidResponse = 3,

    /// <summary>Uncertain distribution.</summary>
    LowConfidence = 4,

    /// <summary>Transport or service unavailable.</summary>
    Unavailable = 5,

    /// <summary>Classifier subdeadline reached.</summary>
    Timeout = 6,

    /// <summary>Another transport operation still owns the slot.</summary>
    Busy = 7,

    /// <summary>Shadow returns only the baseline.</summary>
    Shadow = 8
}
