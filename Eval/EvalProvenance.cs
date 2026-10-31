// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval;

/// <summary>Non-secret settings identifying exactly what a report measured.</summary>
/// <param name="Mode">Offline or paid.</param>
/// <param name="Evidence">Synthetic or measured.</param>
/// <param name="FixtureHash">Hash of the golden set and fixture responses.</param>
/// <param name="CorpusHash">Hash of the corpus manifest.</param>
/// <param name="SourceRevision">Source revision supplied by the runner.</param>
/// <param name="AnswerModel">Pinned answer model.</param>
/// <param name="JudgeModel">Pinned judge model.</param>
/// <param name="EmbeddingModel">Pinned embedding model.</param>
/// <param name="Evaluator">Pinned evaluation task version.</param>
internal sealed record EvalProvenance(string Mode, string Evidence, string FixtureHash, string CorpusHash, string SourceRevision, string AnswerModel, string JudgeModel, string EmbeddingModel, string Evaluator);
