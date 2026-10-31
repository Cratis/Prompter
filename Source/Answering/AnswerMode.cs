// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Represents the shape of answer a question asks for.</summary>
public enum AnswerMode
{
    /// <summary>A regular documentation answer.</summary>
    Standard = 0,

    /// <summary>A "which product or package should I use" question that gets a recommendation-shaped answer.</summary>
    Recommendation = 1
}
