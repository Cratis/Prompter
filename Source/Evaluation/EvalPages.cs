// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Evaluation;

/// <summary>Normalizes documentation mirrors and canonical URLs for comparison.</summary>
public static class EvalPages
{
    /// <summary>Removes the scheme, www host twin, mirror, index and slash suffixes.</summary>
    /// <param name="url">The documentation URL.</param>
    /// <returns>A case-insensitive comparison key.</returns>
    public static string Normalize(string url)
    {
        var value = url.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && IsWebScheme(uri))
        {
            value = value[(value.IndexOf("://", StringComparison.Ordinal) + 3)..];
            if (value.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) value = value[4..];
        }
        if (value.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) value = value[..^3];
        value = value.TrimEnd('/');
        if (value.EndsWith("/index", StringComparison.OrdinalIgnoreCase)) value = value[..^6];
        return value.TrimEnd('/').ToUpperInvariant();
    }

    static bool IsWebScheme(Uri uri) => uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
}
