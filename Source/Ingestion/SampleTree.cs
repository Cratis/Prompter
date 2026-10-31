// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>Represents a GitHub recursive tree response.</summary>
/// <param name="Truncated">Whether GitHub omitted entries.</param>
/// <param name="Tree">The entries returned by GitHub.</param>
public record SampleTree(bool Truncated, IReadOnlyList<SampleTreeEntry> Tree);
