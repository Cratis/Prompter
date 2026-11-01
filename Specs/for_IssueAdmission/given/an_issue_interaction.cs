// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueAdmission.given;

public class an_issue_interaction : for_ActionButtons.given.an_action_interaction
{
    protected IIssues Issues { get; private set; } = null!;
    protected IIssueDrafting Drafting { get; private set; } = null!;
    protected PendingIssues Pending { get; private set; } = null!;
    protected PendingIssue Payload { get; } = new("Cratis", "Chronicle", "Title", "Reviewed body", ["from-discord"]);
    protected IssueConsent Consent { get; } = new(3, 1, 70);

    protected async Task<string> Confirm(ulong? parent, IssueConsent? consent = null, ulong preview = 90)
    {
        var token = await Pending.Present(Payload, consent ?? Consent, _ => Task.FromResult(preview));
        CreateContext(parent, IssueButton.CustomId(IssueButton.FileAction, token));
        var module = new IssueConfirmation(Issues, Pending, Admission, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<IssueConfirmation>.Instance);
        Bind(module);
        await module.Confirm();
        return token;
    }

    protected async Task Draft(ulong? parent)
    {
        CreateContext(parent);
        var module = new FileIssue(Drafting, Issues, Pending, Admission, Work, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<FileIssue>.Instance);
        Bind(module);
        await module.Handle("A reproducible problem");
    }

    void Establish()
    {
        Options.Protection.GitHubWritesEnabled = true;
        Options.GitHub.AppId = 1;
        Options.GitHub.InstallationId = 2;
        Options.GitHub.AppPrivateKeyPath = "unused-test-key-path";
        Options.GitHub.InstallationRepositories = ["Cratis/Chronicle"];
        Pending = new(Clock);
        Issues = Substitute.For<IIssues>();
        Issues.File(Arg.Any<PendingIssue>(), Arg.Any<CancellationToken>()).Returns(new FiledIssue(1, "Chronicle", "https://github.com/Cratis/Chronicle/issues/1"));
        Issues.FindSimilar(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<ExistingIssue>>([]));
        Drafting = Substitute.For<IIssueDrafting>();
        Drafting.Draft(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(new IssueDraft("Title", "Body", IssueKind.Bug, "chronicle"));
    }
}
