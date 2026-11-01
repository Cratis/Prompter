// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_parent_is_missing : given.an_issue_interaction
{
    readonly List<string> _tokens = [];

    async Task Because()
    {
        foreach (var parent in (ulong?[])[null, 0])
        {
            _tokens.Add(await Confirm(parent));
            await Draft(parent);
        }
    }

    [Fact] void should_not_read_or_write_to_discord() => Paths.ShouldBeEmpty();
    [Fact] void should_not_acknowledge_or_deliver_a_preview() => Payloads.ShouldBeEmpty();
    [Fact] void should_not_draft() => Drafting.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_search_or_file_any_issue() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reserve_paid_work() => Ledger.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_leave_the_bound_consent_unconsumed() => _tokens.ShouldEachConformTo(token => Pending.Take(token, Consent, 90) == Payload);
}
