// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_ReadmeMarkdown.when_cleaning;

public class and_the_readme_has_badges_and_generated_blocks : Specification
{
    string _clean = null!;
    void Because() => _clean = ReadmeMarkdown.Clean(
        """
        [![Badge](https://badge.invalid)](https://target.invalid)
        <!-- stats:start -->Volatile stats<!-- stats:end -->
        <!-- hidden -->
        <b>Visible</b>

        ```html
        <p>Code sample</p>
        <!-- keep code comments -->
        ```
        """,
        "Cratis/Arc",
        "A \"quoted\"\nsummary",
        1000);
    [Fact] void should_remove_badges_comments_and_generated_stats() => _clean.Contains("http", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_keep_inner_text_and_code_tags() => _clean.ShouldContain("Visible\n\n```html\n<p>Code sample</p>\n<!-- keep code comments -->\n```");
    [Fact] void should_supply_safe_frontmatter_and_a_title() => _clean.StartsWith("---\ntitle: \"Cratis/Arc\"\ndescription: \"A 'quoted' summary\"\n---\n# Cratis/Arc", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_truncate_the_body_at_a_line_break() => ReadmeMarkdown.Clean("First line\nSecond line\nThird line", "Cratis/Arc", string.Empty, 15).EndsWith("First line", StringComparison.Ordinal).ShouldBeTrue();
}
