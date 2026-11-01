// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_parent_is_allowed_for_confirmation : given.an_issue_interaction
{
    string _token = null!;

    async Task Because() => _token = await Confirm(7);

    [Fact] void should_file_the_reviewed_payload_once() => Issues.Received(1).File(Payload, Arg.Any<CancellationToken>());
    [Fact] void should_consume_the_bound_consent() => Pending.Take(_token, Consent, 90).ShouldBeNull();
    [Fact] void should_not_fetch_channel_history() => Paths.Any(path => path.Contains("/channels/", StringComparison.Ordinal)).ShouldBeFalse();
}
