// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_DatabaseRoleName.when_creating;

public class and_the_name_is_a_plain_identifier : Specification
{
    DatabaseRoleName _result = null!;

    void Because() => _result = new DatabaseRoleName("prompter_app");

    [Fact] void should_keep_the_value() => _result.Value.ShouldEqual("prompter_app");
}
