// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_both_are_configured : Specification
{
    IReadOnlyDictionary<string, string> _values = null!;

    void Because() => _values = DiscordBehaviorEnvironment.Values(" 77 ", "10, 20,,30");

    [Fact] void should_project_the_role() => _values["Cratis__Prompter__Discord__MaintainerRoleId"].ShouldEqual("77");
    [Fact] void should_project_each_channel_by_index()
    {
        _values["Cratis__Prompter__Discord__AnswerInThreadChannelIds__0"].ShouldEqual("10");
        _values["Cratis__Prompter__Discord__AnswerInThreadChannelIds__1"].ShouldEqual("20");
        _values["Cratis__Prompter__Discord__AnswerInThreadChannelIds__2"].ShouldEqual("30");
    }
    [Fact] void should_project_nothing_else() => _values.Count.ShouldEqual(4);
}
