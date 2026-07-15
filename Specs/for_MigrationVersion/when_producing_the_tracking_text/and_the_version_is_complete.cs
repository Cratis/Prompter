// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationVersion.when_producing_the_tracking_text;

public class and_the_version_is_complete : Specification
{
    string _result = null!;

    void Because() => _result = new MigrationVersion(1, 1, 0).ToString();

    [Fact] void should_render_the_canonical_dotted_form() => _result.ShouldEqual("1.1.0");
    [Fact] void should_round_trip_through_parse() => MigrationVersion.Parse(_result).ShouldEqual(new MigrationVersion(1, 1, 0));
}
