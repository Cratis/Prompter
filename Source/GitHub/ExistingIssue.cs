// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// An open issue that already exists, used to offer "this looks like #123" before a duplicate is opened.
/// </summary>
/// <param name="Number">The issue number.</param>
/// <param name="Title">The issue title.</param>
/// <param name="Url">The issue's web address.</param>
public record ExistingIssue(int Number, string Title, string Url);
