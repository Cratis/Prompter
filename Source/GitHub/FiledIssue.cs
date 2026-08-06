// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// An issue that now exists on GitHub.
/// </summary>
/// <param name="Number">The issue number within its repository.</param>
/// <param name="Repository">The repository it was filed in, without the owner.</param>
/// <param name="Url">The issue's web address, which is what a reporter is shown.</param>
public record FiledIssue(int Number, string Repository, string Url);
