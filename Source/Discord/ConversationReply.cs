// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Discord;

/// <summary>The original documentation answer plus separate untrusted advisory hints.</summary>
/// <param name="Answer">The unchanged documentation pipeline result.</param>
/// <param name="Related">Canonical links, never citations.</param>
/// <param name="Offer">An optional explicit-command invitation, never consent.</param>
public sealed record ConversationReply(Answer Answer, IReadOnlyList<RelatedIssue> Related, string Offer);
