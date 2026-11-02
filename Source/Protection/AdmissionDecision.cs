// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>A local admission result, containing no identifying data.</summary>
/// <param name="Allowed">Whether costly work may begin.</param>
/// <param name="Reply">Whether the throttled refusal may be delivered.</param>
/// <param name="Text">The refusal text naming the limit, or <see langword="null"/> to use the generic reply.</param>
public sealed record AdmissionDecision(bool Allowed, bool Reply, string? Text = null);
