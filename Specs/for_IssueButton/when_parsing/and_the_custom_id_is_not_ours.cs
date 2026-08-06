// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_IssueButton.when_parsing;

public class and_the_custom_id_is_not_ours : Specification
{
    [Fact] void should_reject_a_foreign_prefix() => (IssueButton.Parse("feedback:up:1") is null).ShouldBeTrue();
    [Fact] void should_reject_an_unknown_action() => (IssueButton.Parse("issue:delete:1") is null).ShouldBeTrue();
    [Fact] void should_reject_a_missing_token() => (IssueButton.Parse("issue:file:") is null).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_shape() => (IssueButton.Parse("issue:file") is null).ShouldBeTrue();
}
