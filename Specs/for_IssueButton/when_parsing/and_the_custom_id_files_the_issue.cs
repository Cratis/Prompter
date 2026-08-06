// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_IssueButton.when_parsing;

public class and_the_custom_id_files_the_issue : Specification
{
    IssueButtonClick _click = null!;

    void Because() => _click = IssueButton.Parse(IssueButton.CustomId(IssueButton.FileAction, "17"))!;

    [Fact] void should_decode_a_click() => _click.ShouldNotBeNull();
    [Fact] void should_round_trip_the_token() => _click.Token.ShouldEqual("17");
    [Fact] void should_know_it_files() => _click.Files.ShouldBeTrue();
}
