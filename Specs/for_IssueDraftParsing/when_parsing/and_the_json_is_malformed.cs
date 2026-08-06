// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueDraftParsing.when_parsing;

public class and_the_json_is_malformed : Specification
{
    IssueDraft? _draft;

    void Because() => _draft = IssueDraftParsing.Parse("""{"title":"A title", "body":""");

    [Fact] void should_not_draft_anything() => (_draft is null).ShouldBeTrue();
}
