// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Describes a failed evaluation question for the report. The report is uploaded as a public workflow
/// artifact, which log masking does not cover, so only the exception type is recorded: provider and SDK
/// messages can echo request details, including credentials.
/// </summary>
public static class EvalError
{
    /// <summary>
    /// Describes an exception without its message.
    /// </summary>
    /// <param name="exception">The exception that failed the question.</param>
    /// <returns>The exception's type name.</returns>
    public static string Describe(Exception exception) => exception.GetType().Name;
}
