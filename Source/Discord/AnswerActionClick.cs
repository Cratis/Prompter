// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>A decoded, content-free answer action.</summary>
/// <param name="Action">The action prefix.</param>
/// <param name="Question">The question location.</param>
/// <param name="Product">The optional routing product.</param>
public sealed record AnswerActionClick(string Action, QuestionLink Question, string? Product);
