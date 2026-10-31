// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>Controls the curated public sample corpus.</summary>
public class SamplesOptions
{
    /// <summary>Gets or sets whether curated samples are included.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the public owner/repository to read.</summary>
    public string Repository { get; set; } = "Cratis/Samples";

    /// <summary>Gets or sets the branch to read.</summary>
    public string Branch { get; set; } = "main";

    /// <summary>Gets or sets the case-sensitive curated path globs.</summary>
    public IList<string> Paths { get; set; } =
    [
        "README.md", "LearningPath/**/README.md", "LearningPath/**/*.cs", "Chronicle/**/README.md",
        "Arc/**/README.md", "EventModeling/**/README.md", "ModelFirst/**/README.md", "GeneratedValues/**/README.md",
        "Capstone/README.md", "Library/README.md"
    ];

    /// <summary>Gets or sets the maximum accepted file size in bytes.</summary>
    public int MaxFileBytes { get; set; } = 16_384;

    /// <summary>Gets or sets the maximum number of files fetched per run.</summary>
    public int MaxFiles { get; set; } = 300;
}
