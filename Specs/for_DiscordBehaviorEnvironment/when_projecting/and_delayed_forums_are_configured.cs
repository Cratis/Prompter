// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_delayed_forums_are_configured : Specification
{
    IReadOnlyDictionary<string, string> _values = null!;

    void Because() => _values = DiscordBehaviorEnvironment.Values(null, null, "10, 20:45");

    [Fact] void should_project_each_forum_by_index()
    {
        _values["Cratis__Prompter__Discord__DelayedForums__0__ChannelId"].ShouldEqual("10");
        _values["Cratis__Prompter__Discord__DelayedForums__1__ChannelId"].ShouldEqual("20");
    }
    [Fact] void should_default_the_delay_to_thirty_minutes() => _values["Cratis__Prompter__Discord__DelayedForums__0__DelayMinutes"].ShouldEqual("30");
    [Fact] void should_project_the_given_delay() => _values["Cratis__Prompter__Discord__DelayedForums__1__DelayMinutes"].ShouldEqual("45");
    [Fact] void should_project_nothing_else() => _values.Count.ShouldEqual(4);
}
