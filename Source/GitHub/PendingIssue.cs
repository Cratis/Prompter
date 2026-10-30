// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// The complete, immutable payload shown before a public GitHub write.
/// </summary>
/// <param name="Owner">The repository owner.</param>
/// <param name="Repository">The repository name.</param>
/// <param name="Title">The exact title to post.</param>
/// <param name="Body">The exact body, including provenance, to post.</param>
/// <param name="Labels">The exact labels to apply.</param>
public sealed record PendingIssue(string Owner, string Repository, string Title, string Body, ImmutableArray<string> Labels);
