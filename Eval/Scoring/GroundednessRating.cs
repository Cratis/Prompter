// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Represents the structured verdict the LLM judge returns when scoring how well an answer is grounded in the
/// retrieved documentation passages.
/// </summary>
/// <param name="Score">The groundedness score from 1 (unsupported / hallucinated) to 5 (fully supported).</param>
/// <param name="Reason">A short justification for the score.</param>
public sealed record GroundednessRating(int Score, string Reason);
