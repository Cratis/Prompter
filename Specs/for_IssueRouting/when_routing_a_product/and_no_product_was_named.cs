// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueRouting.when_routing_a_product;

public class and_no_product_was_named : Specification
{
    readonly GitHubOptions _options = new();
    string _repository = null!;

    void Because() => _repository = IssueRouting.RepositoryFor(string.Empty, _options);

    [Fact] void should_fall_back_to_the_default() => _repository.ShouldEqual(_options.DefaultRepository);
    [Fact] void should_not_consider_it_routed() => IssueRouting.IsRouted(string.Empty, _options).ShouldBeFalse();
    [Fact] void should_not_consider_null_routed() => IssueRouting.IsRouted(null, _options).ShouldBeFalse();
}
