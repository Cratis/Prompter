// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Classifies corpus pages without changing stored metadata.</summary>
public static class SourceKinds
{
    /// <summary>Gets the kind of guidance at a page URL.</summary>
    /// <param name="page">The source URL.</param>
    /// <returns>The source kind.</returns>
    public static SourceKind Of(PageUrl page)
    {
        if (!Uri.TryCreate(page.Value, UriKind.Absolute, out var url) || !IsGitHub(url))
        {
            return SourceKind.Documentation;
        }

        var segments = url.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 4 || !segments[0].Equals("Cratis", StringComparison.OrdinalIgnoreCase))
        {
            return SourceKind.Documentation;
        }

        return Kind(segments);
    }

    static bool IsGitHub(Uri url) => url.Scheme == Uri.UriSchemeHttps && url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase);

    static bool IsRelease(string[] segments) => segments[2] == "releases" && segments[3] == "tag";

    static SourceKind Kind(string[] segments)
    {
        if (segments.Length > 4 && IsRelease(segments))
        {
            return SourceKind.ReleaseNotes;
        }

        return segments[1].Equals("Samples", StringComparison.OrdinalIgnoreCase) && segments[2] == "blob"
            ? SourceKind.Sample : SourceKind.Documentation;
    }
}
