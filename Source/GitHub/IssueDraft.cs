// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// An issue Prompter has drafted from a conversation but not yet filed. It exists only between the draft and
/// the reporter confirming it — nothing persists a draft, which is what keeps filing consent-in-the-moment
/// (decision D-13 stays intact because no conversation content is ever stored).
/// </summary>
/// <param name="Title">The issue title.</param>
/// <param name="Body">The issue body, as the model wrote it — the surrounding context is added when filing.</param>
/// <param name="Kind">The kind of work the issue represents.</param>
/// <param name="Product">The product the issue belongs to, used to route it to a repository.</param>
public record IssueDraft(string Title, string Body, IssueKind Kind, string Product);
