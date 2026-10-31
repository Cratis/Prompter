// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Identifies the guidance a corpus page provides.</summary>
public enum SourceKind
{
    /// <summary>Published documentation.</summary>
    Documentation = 0,

    /// <summary>Published release notes.</summary>
    ReleaseNotes = 1,

    /// <summary>A curated sample file.</summary>
    Sample = 2
}
