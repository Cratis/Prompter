// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Aggregate provider exposure only; never a person, channel, or prompt.</summary>
/// <param name="Anthropic">Upper Anthropic dollar exposure.</param>
/// <param name="Voyage">Upper Voyage dollar exposure.</param>
public sealed record SpendExposure(decimal Anthropic, decimal Voyage)
{
    /// <summary>Gets the shared global exposure.</summary>
    public decimal Total => Anthropic + Voyage;

    /// <summary>Gets a value indicating whether the exposure is a positive bounded amount.</summary>
    public bool IsValid => Anthropic >= 0 && Voyage >= 0 && Total > 0;
}
