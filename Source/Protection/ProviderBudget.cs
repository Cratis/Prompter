// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Verified upper prices (including cache writes) and the model's hard input ceiling.</summary>
public class ProviderBudget
{
    /// <summary>Gets or sets the exact model for which prices and limits were verified.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Gets or sets the upper input dollar price per million tokens.</summary>
    public decimal InputUsdPerMillion { get; set; }

    /// <summary>Gets or sets the upper output dollar price per million tokens.</summary>
    public decimal OutputUsdPerMillion { get; set; }

    /// <summary>Gets or sets the hard input token ceiling. Zero means unverified.</summary>
    public int MaxInputTokens { get; set; }

    /// <summary>Gets or sets the provider UTC-day sublimit, always beneath the shared ceiling.</summary>
    public decimal DailyUsd { get; set; } = 5;

    /// <summary>Calculates a conservative cost.</summary>
    /// <param name="input">Input token ceiling or known usage.</param>
    /// <param name="output">Output token ceiling or known usage.</param>
    /// <returns>The dollar exposure.</returns>
    public decimal Cost(long input, long output = 0) =>
        ((input * InputUsdPerMillion) + (output * OutputUsdPerMillion)) / 1000000m;

    /// <summary>Checks that a model's required price data is present.</summary>
    /// <param name="model">The effective model.</param>
    /// <param name="output">Whether output prices are required.</param>
    /// <returns>Whether dispatch can be bounded.</returns>
    public bool Covers(string model, bool output) =>
        string.Equals(Model, model, StringComparison.Ordinal) && InputUsdPerMillion > 0 &&
        (!output || OutputUsdPerMillion > 0) && MaxInputTokens > 0 && DailyUsd > 0;
}
