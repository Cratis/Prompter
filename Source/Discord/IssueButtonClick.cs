// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// A decoded click on one of the buttons under an issue preview.
/// </summary>
/// <param name="Action">The action clicked, either <see cref="IssueButton.FileAction"/> or <see cref="IssueButton.CancelAction"/>.</param>
/// <param name="Token">The token identifying the held draft.</param>
public record IssueButtonClick(string Action, string Token)
{
    /// <summary>
    /// Gets a value indicating whether the click files the issue.
    /// </summary>
    public bool Files => Action == IssueButton.FileAction;
}
