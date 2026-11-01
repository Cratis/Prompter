// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.when_authorizing;

public class and_the_preview_binding_is_wrong : given.an_issue_interaction
{
    string _token = null!;

    async Task Because() => _token = await Confirm(7, preview: 91);

    [Fact] void should_not_file_or_search() => Issues.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_consume_another_previews_consent() => Pending.Take(_token, Consent, 91).ShouldEqual(Payload);
}
