// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Represents one public GitHub release.</summary>
/// <param name="TagName">The published version tag.</param>
/// <param name="Name">The optional display name.</param>
/// <param name="Body">The release notes.</param>
/// <param name="Draft">Whether this is a draft.</param>
/// <param name="Prerelease">Whether this is a prerelease.</param>
/// <param name="PublishedAt">The explicit publication date.</param>
/// <param name="HtmlUrl">The public release URL.</param>
public record ReleaseNote(string TagName, string? Name, string? Body, bool Draft, bool Prerelease, DateTimeOffset? PublishedAt, string HtmlUrl);
