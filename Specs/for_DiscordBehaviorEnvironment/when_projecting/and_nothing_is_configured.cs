// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_nothing_is_configured : Specification
{
    IReadOnlyDictionary<string, string> _values = null!;

    void Because() => _values = DiscordBehaviorEnvironment.Values(null, " ");

    [Fact] void should_project_nothing() => _values.Count.ShouldEqual(0);
}
