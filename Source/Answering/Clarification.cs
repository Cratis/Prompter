// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Represents a "which product do you mean?" question asked before spending a model call.</summary>
/// <param name="Options">The candidate products, best evidence first.</param>
public sealed record Clarification(IReadOnlyList<InteractionProduct> Options);
