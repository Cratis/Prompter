// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Controls the public Cratis repository README corpus.
/// </summary>
public class ReadmesOptions
{
    /// <summary>Gets or sets whether READMEs are indexed.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets whether the organization profile is included.</summary>
    public bool IncludeProfile { get; set; } = true;

    /// <summary>Gets or sets the maximum repositories inspected per run.</summary>
    public int MaxRepositories { get; set; } = 200;

    /// <summary>Gets or sets the maximum retained README characters.</summary>
    public int MaxReadmeCharacters { get; set; } = 32_768;

    /// <summary>Gets or sets repository names excluded from ingestion.</summary>
    public IList<string> ExcludedRepositories { get; set; } = [".github"];

    /// <summary>Gets whether the ingestion bounds are positive.</summary>
    public bool IsValid => MaxRepositories > 0 && MaxReadmeCharacters > 0;
}
