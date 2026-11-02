// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_DatabaseRoleName.when_creating;

public class and_the_name_is_not_acceptable
{
    [Theory]
    [InlineData("Prompter")]
    [InlineData("a-b")]
    [InlineData("x;drop")]
    [InlineData("")]
    [InlineData("1abc")]
    public void should_reject_the_value(string value) =>
        Assert.Throws<InvalidDatabaseRoleName>(() => new DatabaseRoleName(value));

    [Fact]
    public void should_reject_a_name_over_63_characters() =>
        Assert.Throws<InvalidDatabaseRoleName>(() => new DatabaseRoleName(new string('a', 64)));

    [Fact]
    public void should_accept_a_name_of_63_characters() =>
        new DatabaseRoleName(new string('a', 63)).Value.Length.ShouldEqual(63);
}
