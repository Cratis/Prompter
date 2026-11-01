// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_reporter_is_wrong : given.an_issue_interaction
{
    readonly IssueConsent _owner = new(4, 1, 70);
    string _token = null!;

    async Task Because() => _token = await Confirm(7, _owner);

    [Fact] void should_not_file_or_search() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_consume_another_reporters_consent() => Pending.Take(_token, _owner, 90).ShouldEqual(Payload);
}
