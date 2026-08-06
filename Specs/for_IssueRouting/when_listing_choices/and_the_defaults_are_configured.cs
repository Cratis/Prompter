// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueRouting.when_listing_choices;

public class and_the_defaults_are_configured : Specification
{
    readonly GitHubOptions _options = new();
    IReadOnlyList<string> _choices = null!;

    void Because() => _choices = IssueRouting.Choices(_options);

    [Fact] void should_offer_the_default_first() => _choices[0].ShouldEqual(_options.DefaultRepository);
    [Fact] void should_not_repeat_the_default() => _choices.Count(choice => choice == _options.DefaultRepository).ShouldEqual(1);
    [Fact] void should_offer_every_product_repository() => _choices.ShouldContain("Chronicle");
}
