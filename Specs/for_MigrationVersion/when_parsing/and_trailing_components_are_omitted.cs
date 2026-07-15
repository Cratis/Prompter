// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationVersion.when_parsing;

public class and_trailing_components_are_omitted : Specification
{
    MigrationVersion _result = null!;

    void Because() => _result = MigrationVersion.Parse("v2");

    [Fact] void should_read_the_major() => _result.Major.ShouldEqual(2);
    [Fact] void should_default_the_minor_to_zero() => _result.Minor.ShouldEqual(0);
    [Fact] void should_default_the_patch_to_zero() => _result.Patch.ShouldEqual(0);
}
