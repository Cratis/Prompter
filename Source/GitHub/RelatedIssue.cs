// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>A bounded hint, never documentation evidence or proof of a fix. No remote title/body is retained.</summary>
/// <param name="Number">The validated issue number.</param>
/// <param name="Url">The locally constructed canonical issue link.</param>
/// <param name="RecentlyClosed">Whether the issue was closed in the last 90 days.</param>
public sealed record RelatedIssue(int Number, string Url, bool RecentlyClosed);
