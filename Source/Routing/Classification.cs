// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Untrusted advisory data; never instructions, evidence, or filing consent.</summary>
/// <param name="Product">A configured product key or unknown.</param>
/// <param name="Intent">The finite intent.</param>
/// <param name="Action">An advisory branch only.</param>
/// <param name="Confidence">Uncalibrated confidence bucket.</param>
/// <param name="Source">The data source.</param>
/// <param name="Fallback">A bounded fallback reason.</param>
public sealed record Classification(
    RoutingProduct Product,
    ClassificationIntent Intent,
    ClassificationAction Action,
    ConfidenceBucket Confidence,
    ClassificationSource Source,
    ClassificationFallback Fallback);
