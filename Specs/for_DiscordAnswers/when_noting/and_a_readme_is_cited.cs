// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_noting;

public class and_a_readme_is_cited : Specification
{
    string _formatted = null!;
    void Because() => _formatted = DiscordAnswers.Format(new Answer("Overview.", [new("https://github.com/Cratis/Arc/blob/main/README.md")], 0.9, false, []));
    [Fact] void should_keep_the_blob_link_and_label() => _formatted.ShouldContain("<https://github.com/Cratis/Arc/blob/main/README.md> (README)");
}
