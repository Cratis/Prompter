// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Represents the URL of a documentation page.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record PageUrl(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value used when a page URL is not set.
    /// </summary>
    public static readonly PageUrl NotSet = new(string.Empty);

    public static implicit operator string(PageUrl url) => url.Value;

    public static implicit operator PageUrl(string value) => new(value);
}
