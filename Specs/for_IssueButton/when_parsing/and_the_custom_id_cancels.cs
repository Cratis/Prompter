// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_IssueButton.when_parsing;

public class and_the_custom_id_cancels : Specification
{
    IssueButtonClick _click = null!;

    void Because() => _click = IssueButton.Parse(IssueButton.CustomId(IssueButton.CancelAction, "17"))!;

    [Fact] void should_decode_a_click() => _click.ShouldNotBeNull();
    [Fact] void should_not_file() => _click.Files.ShouldBeFalse();
}
