// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI.Evaluation;

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Supplies the retrieved documentation passages an answer must be grounded in to the
/// <see cref="GroundednessEvaluator"/>, following the additional-context pattern of Microsoft.Extensions.AI's
/// evaluators.
/// </summary>
/// <param name="groundingContext">The concatenated retrieved passages the answer should be supported by.</param>
internal sealed class GroundednessContext(string groundingContext)
    : EvaluationContext(ContextName, content: groundingContext)
{
    /// <summary>The name this context is registered under.</summary>
    public const string ContextName = "Grounding Context";

    /// <summary>Gets the concatenated retrieved passages the answer should be supported by.</summary>
    public string GroundingContext { get; } = groundingContext;
}
