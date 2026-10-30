// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueConfirmation.when_submitting;

public class and_delivery_has_been_confirmed : Specification
{
    readonly IIssues _issues = Substitute.For<IIssues>();
    readonly PendingIssue _payload = new("Cratis", "Chronicle", "Title", "Final body and provenance", ["from-discord", "bug"]);
    string _wrong = null!;
    string _filed = null!;
    string _replay = null!;

    async Task Because()
    {
        var options = new GitHubOptions();
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(_payload, scope, _ => Task.FromResult(4UL));
        var confirmation = new IssueConfirmation(_issues, pending, null!, Options.Create(new PrompterOptions { GitHub = options }), NullLogger<IssueConfirmation>.Instance);
        _issues.File(_payload, Arg.Any<CancellationToken>()).Returns(new FiledIssue(1, "Chronicle", "https://github.com/Cratis/Chronicle/issues/1"));
        options.Owner = "Changed";
        options.IssueLabel = "Changed";
        _wrong = await confirmation.Submit(new(IssueButton.FileAction, token), new(9, 2, 3), 4, CancellationToken.None);
        _filed = await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, CancellationToken.None);
        _replay = await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, CancellationToken.None);
    }

    [Fact] void should_not_reveal_another_reporters_payload() => _wrong.ShouldEqual(IssueConfirmation.ExpiredReply);
    [Fact] void should_return_the_created_url() => _filed.ShouldEqual("Filed: https://github.com/Cratis/Chronicle/issues/1");
    [Fact] void should_reject_replay() => _replay.ShouldEqual(IssueConfirmation.ExpiredReply);
    [Fact] void should_file_the_exact_snapshot_only_once() => _issues.Received(1).File(_payload, Arg.Any<CancellationToken>());
    [Fact] void should_never_call_the_legacy_mutable_owner_api() => _issues.DidNotReceive().File(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
}
