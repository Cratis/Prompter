// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueComposition.when_building_labels;

public class and_the_issue_is_a_bug : Specification
{
    readonly GitHubOptions _options = new();
    IReadOnlyList<string> _labels = null!;

    void Because() => _labels = IssueComposition.Labels(new("Title", "Body", IssueKind.Bug, "chronicle"), _options);

    [Fact] void should_carry_the_provenance_label() => _labels.ShouldContain(_options.IssueLabel);
    [Fact] void should_carry_the_kind_label() => _labels.ShouldContain("bug");
}
