// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_IssueButton.when_building_a_custom_id;

public class and_the_action_files_the_issue : Specification
{
    string _customId = null!;

    void Because() => _customId = IssueButton.CustomId(IssueButton.FileAction, "17");

    [Fact] void should_encode_prefix_action_and_token() => _customId.ShouldEqual("issue:file:17");
    [Fact] void should_stay_within_discords_custom_id_limit() => (_customId.Length <= 100).ShouldBeTrue();
}
