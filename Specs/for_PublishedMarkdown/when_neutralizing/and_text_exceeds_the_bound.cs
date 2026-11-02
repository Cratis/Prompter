// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_PublishedMarkdown.when_neutralizing;

public class and_text_exceeds_the_bound : Specification
{
    string _result = string.Empty;

    void Because() => _result = PublishedMarkdown.Neutralize(new string('a', PublishedMarkdown.MaximumLength) + " [click](https://evil.invalid) www.evil.invalid @everyone <@1>", new());

    [Fact] void should_defang_every_destination_and_mention() => (_result.Contains("://evil", StringComparison.Ordinal) || _result.Contains("www.evil", StringComparison.Ordinal) || _result.Contains("@everyone", StringComparison.Ordinal) || _result.Contains("<@", StringComparison.Ordinal)).ShouldBeFalse();
}
