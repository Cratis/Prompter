// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_user_lists_are_configured : Specification
{
    IReadOnlyDictionary<string, string> _values = null!;
    Exception? _badDenied;
    Exception? _badAllowed;

    void Because()
    {
        _values = DiscordBehaviorEnvironment.Values(null, null, null, "10, 20,,", "30");
        _badDenied = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, null, "x"));
        _badAllowed = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, null, null, "-1"));
    }

    [Fact]
    void should_project_each_denied_user_by_index()
    {
        _values["Cratis__Prompter__Protection__DeniedUsers__0"].ShouldEqual("10");
        _values["Cratis__Prompter__Protection__DeniedUsers__1"].ShouldEqual("20");
    }
    [Fact] void should_project_each_allowed_user_by_index() => _values["Cratis__Prompter__Protection__AllowedUsers__0"].ShouldEqual("30");
    [Fact] void should_project_nothing_else() => _values.Count.ShouldEqual(3);
    [Fact] void should_reject_a_denied_id_that_is_not_a_number() => _badDenied.ShouldBeOfExactType<InvalidDiscordId>();
    [Fact] void should_reject_an_allowed_id_that_is_not_a_number() => _badAllowed.ShouldBeOfExactType<InvalidDiscordId>();
}
