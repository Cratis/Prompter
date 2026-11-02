// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents a section of a markdown page: the text under one heading.
/// </summary>
/// <param name="HeadingPath">The heading path of the section.</param>
/// <param name="Content">The text of the section, without its own heading line.</param>
/// <param name="Root">The heading path down to the second level heading the section belongs to (the page title before the first one).</param>
/// <param name="Heading">The section's own heading text, or <see langword="null"/> for the page intro.</param>
sealed record Section(string HeadingPath, string Content, string Root, string? Heading);
