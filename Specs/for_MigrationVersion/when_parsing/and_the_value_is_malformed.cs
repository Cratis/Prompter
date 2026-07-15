// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationVersion.when_parsing;

public class and_the_value_is_malformed : Specification
{
    Exception _exception = null!;

    void Because()
    {
        try
        {
            _ = MigrationVersion.Parse("not-a-version");
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_reject_the_value() => (_exception is MalformedMigrationVersion).ShouldBeTrue();
}
