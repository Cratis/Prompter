// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>A conservative reservation, not measured provider usage.</summary>
/// <param name="Tokens">The retry-inclusive token ceiling.</param>
/// <param name="Dollars">The corresponding dollar ceiling.</param>
public record IndexExposure(long Tokens, decimal Dollars);
