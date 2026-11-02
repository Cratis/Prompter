// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_processing;

public class and_many_users_change_channels : given.a_nonpaid_action
{
    void Establish() => Create();

    async Task Because()
    {
        for (ulong index = 1; index <= 50; index++)
        {
            await Run(new(1, index, index + 100), channel: ChannelAt(index));
        }
    }

    [Fact] void should_share_the_global_cap_across_all_channels_and_users() => Events.Count(item => item == "work").ShouldEqual(12);
    [Fact] void should_never_acknowledge_globally_denied_clicks() => Events.Count(item => item == "defer").ShouldEqual(12);
    [Fact] void should_complete_each_admitted_lifecycle() => Events.Count(item => item == "followup").ShouldEqual(12);
    [Fact] void should_not_create_an_unbudgeted_refusal_path() => Events.Contains("refuse").ShouldBeFalse();
}
