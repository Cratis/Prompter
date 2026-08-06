// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueRouting.when_routing_a_product;

public class and_the_product_is_known : Specification
{
    readonly GitHubOptions _options = new();
    string _repository = null!;

    void Because() => _repository = IssueRouting.RepositoryFor("chronicle", _options);

    [Fact] void should_route_to_the_owning_repository() => _repository.ShouldEqual("Chronicle");
    [Fact] void should_consider_it_routed() => IssueRouting.IsRouted("chronicle", _options).ShouldBeTrue();
}
