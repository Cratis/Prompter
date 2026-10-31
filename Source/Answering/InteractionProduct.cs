// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Represents the Cratis product an interaction concerns; the numeric values are stable storage values.</summary>
public enum InteractionProduct
{
    /// <summary>Another product, or the product is unknown.</summary>
    OtherOrUnknown = 0,

    /// <summary>Cratis Chronicle.</summary>
    Chronicle = 1,

    /// <summary>Cratis Arc.</summary>
    Arc = 2,

    /// <summary>Cratis Fundamentals.</summary>
    Fundamentals = 3,

    /// <summary>Cratis Components.</summary>
    Components = 4,

    /// <summary>The Cratis CLI.</summary>
    Cli = 5
}
