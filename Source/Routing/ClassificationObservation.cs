// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Metadata only: deliberately no strings, products, identifiers, or provider fields.</summary>
/// <param name="Mode">The configured mode.</param>
/// <param name="Intent">The observed intent.</param>
/// <param name="Action">The observed advisory branch.</param>
/// <param name="BaselineAction">The safe comparison branch.</param>
/// <param name="Confidence">The confidence bucket.</param>
/// <param name="Fallback">The fallback reason.</param>
/// <param name="Elapsed">The elapsed bucket.</param>
public sealed record ClassificationObservation(
    ClassificationMode Mode,
    ClassificationIntent Intent,
    ClassificationAction Action,
    ClassificationAction BaselineAction,
    ConfidenceBucket Confidence,
    ClassificationFallback Fallback,
    ElapsedBucket Elapsed);
