// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;

namespace Cratis.Prompter.Specs.for_IssueRouting.when_routing_a_product;

public class and_the_product_is_surrounded_by_whitespace : Specification
{
    string _repository = null!;

    void Because() => _repository = IssueRouting.RepositoryFor("  arc  ", new());

    [Fact] void should_still_route() => _repository.ShouldEqual("Arc");
}
