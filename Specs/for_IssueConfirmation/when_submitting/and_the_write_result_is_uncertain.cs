// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueConfirmation.when_submitting;

public class and_the_write_result_is_uncertain : Specification
{
    readonly IIssues _issues = Substitute.For<IIssues>();
    string _reply = null!;
    string _replay = null!;

    async Task Because()
    {
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var payload = new PendingIssue("Cratis", "Chronicle", "Title", "Body", []);
        var token = await pending.Present(payload, scope, _ => Task.FromResult(4UL));
        var confirmation = new IssueConfirmation(_issues, pending, null!, Options.Create(new PrompterOptions()), NullLogger<IssueConfirmation>.Instance);
        _issues.File(payload, Arg.Any<CancellationToken>()).Returns(Task.FromException<FiledIssue>(new HttpRequestException("Synthetic connection loss after POST")));
        _reply = await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, CancellationToken.None);
        _replay = await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, CancellationToken.None);
    }

    [Fact] void should_not_claim_nothing_was_created() => _reply.ShouldContain("may have been created");
    [Fact] void should_require_checking_the_repository() => _reply.ShouldContain("Check the repository");
    [Fact] void should_consume_the_uncertain_attempt() => _replay.ShouldEqual(IssueConfirmation.ExpiredReply);
    [Fact] void should_never_retry() => _issues.Received(1).File(Arg.Any<PendingIssue>(), Arg.Any<CancellationToken>());
}
