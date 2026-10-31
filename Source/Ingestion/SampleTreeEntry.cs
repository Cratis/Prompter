// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Represents one path in a GitHub tree.</summary>
/// <param name="Path">The repository-relative path.</param>
/// <param name="Type">The Git object type.</param>
/// <param name="Size">The blob size in bytes.</param>
public record SampleTreeEntry(string Path, string Type, long Size);
