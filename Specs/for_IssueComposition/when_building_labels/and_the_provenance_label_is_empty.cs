// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueComposition.when_building_labels;

public class and_the_provenance_label_is_empty : Specification
{
    IReadOnlyList<string> _labels = null!;

    void Because() => _labels = IssueComposition.Labels(
        new("Title", "Body", IssueKind.Documentation, "documentation"),
        new() { IssueLabel = string.Empty });

    [Fact] void should_drop_the_empty_label() => _labels.Contains(string.Empty).ShouldBeFalse();
    [Fact] void should_still_carry_the_kind_label() => _labels.ShouldContain("documentation");
}
