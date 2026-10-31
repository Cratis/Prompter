// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Discord;

/// <summary>Builds and reads product clarification button custom ids. The id carries only a product key.</summary>
public static class ClarificationButton
{
    /// <summary>The first segment of every clarification custom id, and the component route.</summary>
    public const string Prefix = "clarify";

    /// <summary>Builds a custom id.</summary>
    /// <param name="product">The product the button selects.</param>
    /// <returns>The custom id.</returns>
    public static string CustomId(InteractionProduct product) => $"{Prefix}:{ProductScope.Key(product)}";

    /// <summary>Reads a custom id.</summary>
    /// <param name="customId">The clicked custom id.</param>
    /// <returns>The product, or <see langword="null"/> for any other shape.</returns>
    public static InteractionProduct? Parse(string customId)
    {
        var parts = customId.Split(':');
        return parts.Length == 2 && parts[0] == Prefix && ProductScope.TryParseKey(parts[1], out var product) ? product : null;
    }
}
