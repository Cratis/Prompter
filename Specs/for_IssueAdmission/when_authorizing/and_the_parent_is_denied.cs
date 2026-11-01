// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_parent_is_denied : given.an_issue_interaction
{
    string _token = null!;

    async Task Because()
    {
        Options.Protection.DeniedChannels = [7];
        _token = await Confirm(7);
        await Draft(7);
    }

    [Fact] void should_not_read_or_write_to_discord() => Paths.ShouldBeEmpty();
    [Fact] void should_not_acknowledge_or_deliver_a_preview() => Payloads.ShouldBeEmpty();
    [Fact] void should_not_draft() => Drafting.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_search_or_file_any_issue() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reserve_paid_work() => Ledger.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_leave_the_bound_consent_unconsumed() => Pending.Take(_token, Consent, 90).ShouldEqual(Payload);
}
