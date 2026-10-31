// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>One untrusted, anonymized history entry, not a model instruction or source.</summary>
/// <param name="Role">User, or assistant for this bot's own earlier replies.</param>
/// <param name="Content">Sanitized plain text only.</param>
public sealed record ConversationMessage(string Role, string Content);
