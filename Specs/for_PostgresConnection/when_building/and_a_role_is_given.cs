// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Storage;

namespace Cratis.Prompter.Specs.for_PostgresConnection.when_building;

public class and_a_role_is_given : Specification
{
    string _result = null!;

    void Because() => _result = PostgresConnection.For(PostgresDeployment.ApplicationUserName, "placeholder");

    [Fact] void should_connect_as_the_application_role() => _result.ShouldContain("Username=prompter_app;");
    [Fact] void should_not_use_the_bootstrap_role() => _result.ShouldNotContain("Username=prompter;");
}
