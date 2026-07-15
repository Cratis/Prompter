// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Represents a retrieved passage with its relevance score.
/// </summary>
/// <param name="Page">The URL of the page the passage comes from.</param>
/// <param name="Title">The title of the page.</param>
/// <param name="HeadingPath">The heading path within the page.</param>
/// <param name="Content">The markdown content of the passage.</param>
/// <param name="Score">The reciprocal-rank-fusion score across lexical and semantic retrieval.</param>
public record Passage(PageUrl Page, string Title, string HeadingPath, string Content, double Score);
