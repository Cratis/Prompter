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
    Cli = 5,

    /// <summary>Cratis Specifications.</summary>
    Specifications = 6,

    /// <summary>Cratis Stage.</summary>
    Stage = 7,

    /// <summary>Cratis Screenplay.</summary>
    Screenplay = 8,

    /// <summary>Cratis Prologue.</summary>
    Prologue = 9,

    /// <summary>Cratis Studio.</summary>
    Studio = 10,

    /// <summary>Cratis Direct.</summary>
    Direct = 11,

    /// <summary>Cratis Assurance support.</summary>
    Assurance = 12
}
