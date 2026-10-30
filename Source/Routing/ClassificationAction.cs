// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Finite advisory branches; none authorizes a write.</summary>
public enum ClassificationAction
{
    /// <summary>Answer from documentation.</summary>
    Answer = 0,

    /// <summary>Offer drafting help without writing anything.</summary>
    OfferIssue = 1,

    /// <summary>Ask for clarification.</summary>
    Clarify = 2
}
