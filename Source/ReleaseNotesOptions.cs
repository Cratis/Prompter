// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>Controls public GitHub release-note ingestion.</summary>
public class ReleaseNotesOptions
{
    /// <summary>Gets or sets whether published releases are included.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the Cratis repositories whose releases are indexed.</summary>
    public IList<string> Repositories { get; set; } = ["Chronicle", "Arc", "Fundamentals", "Components", "cli"];

    /// <summary>Gets or sets the maximum releases requested per repository.</summary>
    public int MaxReleasesPerRepository { get; set; } = 20;

    /// <summary>Gets or sets the maximum accepted release body length.</summary>
    public int MaxBodyCharacters { get; set; } = 20_000;
}
