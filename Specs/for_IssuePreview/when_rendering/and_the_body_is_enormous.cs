// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssuePreview.when_rendering;

public class and_the_body_is_enormous : Specification
{
    string? _preview;

    void Because() => _preview = IssuePreview.Text(new("Cratis", "Chronicle", "Title", new string('x', 1201), []));

    [Fact] void should_reject_instead_of_truncating() => (_preview is null).ShouldBeTrue();
    [Fact] void should_reject_an_overlong_title() => (IssuePreview.Text(new("Cratis", "Chronicle", new string('x', 161), "Body", [])) is null).ShouldBeTrue();
    [Fact] void should_measure_escaped_length() => (IssuePreview.Text(new("Cratis", "Chronicle", "Title", new string('*', 1100), [])) is null).ShouldBeTrue();
    [Fact] void should_reject_hidden_direction_changes() => (IssuePreview.Text(new("Cratis", "Chronicle", "Title", "a\u202Eb", [])) is null).ShouldBeTrue();
}
