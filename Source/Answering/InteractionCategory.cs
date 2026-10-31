// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents the model's triage hint for an interaction. It is a hint, not a product assertion or an
/// authorization; the numeric values are stable storage values.
/// </summary>
public enum InteractionCategory
{
    /// <summary>The category is unknown, missing or not classifiable.</summary>
    Unknown = 0,

    /// <summary>The question was adequately answered from the documentation.</summary>
    Answered = 1,

    /// <summary>The documentation is missing what the question needs.</summary>
    DocsGap = 2,

    /// <summary>The question asks for a product ability that appears to be absent.</summary>
    MissingCapability = 3,

    /// <summary>The question describes a suspected malfunction.</summary>
    LikelyBug = 4,

    /// <summary>The question is not a Cratis request.</summary>
    OutOfScope = 5
}
