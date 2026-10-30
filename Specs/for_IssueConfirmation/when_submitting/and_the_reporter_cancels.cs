// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueConfirmation.when_submitting;

public class and_the_reporter_cancels : Specification
{
    readonly IIssues _issues = Substitute.For<IIssues>();
    string _cancelled = null!;
    string _filed = null!;

    async Task Because()
    {
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(new("Cratis", "Chronicle", "Title", "Body", []), scope, _ => Task.FromResult(4UL));
        var confirmation = new IssueConfirmation(_issues, pending, null!, Options.Create(new PrompterOptions()), NullLogger<IssueConfirmation>.Instance);
        _cancelled = await confirmation.Submit(new(IssueButton.CancelAction, token), scope, 4, CancellationToken.None);
        _filed = await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, CancellationToken.None);
    }

    [Fact] void should_acknowledge_cancellation() => _cancelled.ShouldEqual(IssueConfirmation.CancelledReply);
    [Fact] void should_reject_later_filing() => _filed.ShouldEqual(IssueConfirmation.ExpiredReply);
    [Fact] void should_not_call_github() => _issues.DidNotReceive().File(Arg.Any<PendingIssue>(), Arg.Any<CancellationToken>());
}
