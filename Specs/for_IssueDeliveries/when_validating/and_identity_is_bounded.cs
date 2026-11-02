// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_IssueDeliveries.when_validating;

public class and_identity_is_bounded : Specification
{
    [Theory]
    [InlineData("Cratis/Arc", 1, true)]
    [InlineData("Cratis/Arc", 0, false)]
    [InlineData("Cratis/Arc", -1, false)]
    [InlineData("", 1, false)]
    [InlineData("Cratis", 1, false)]
    [InlineData("Cratis/Arc/extra", 1, false)]
    [InlineData("Cratis/..", 1, false)]
    [InlineData("Cratis/Arc\n", 1, false)]
    public void should_require_a_safe_repository_and_positive_issue_number(string repository, int number, bool valid) =>
        IssueDeliveries.IsValid(repository, number).ShouldEqual(valid);

    [Fact] void should_reject_an_oversized_repository() => IssueDeliveries.IsValid($"Cratis/{new string('a', 200)}", 1).ShouldBeFalse();
}
