// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DocsGapReport.when_composing;

public class and_the_week_is_empty : Specification
{
    string _text = null!;
    void Because() => _text = DocsGapReport.Compose([], new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), new(2026, 11, 9, 8, 0, 0, TimeSpan.Zero));
    [Fact] void should_show_the_window() => _text.ShouldContain("**Prompter docs-gap digest** · 2026-11-02 → 2026-11-09 (UTC)");
    [Fact] void should_confirm_an_empty_week() => _text.ShouldContain("No docs gaps, possible missing capabilities, likely bugs or thumbs-down answers this week.");
}
