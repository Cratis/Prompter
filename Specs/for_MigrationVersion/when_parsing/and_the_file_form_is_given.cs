// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationVersion.when_parsing;

public class and_the_file_form_is_given : Specification
{
    MigrationVersion _result = null!;

    void Because() => _result = MigrationVersion.Parse("v1_2_3");

    [Fact] void should_read_the_major() => _result.Major.ShouldEqual(1);
    [Fact] void should_read_the_minor() => _result.Minor.ShouldEqual(2);
    [Fact] void should_read_the_patch() => _result.Patch.ShouldEqual(3);
}
