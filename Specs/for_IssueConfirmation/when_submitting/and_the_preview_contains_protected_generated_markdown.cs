// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IssueConfirmation.when_submitting;

public class and_the_preview_contains_protected_generated_markdown : Specification
{
    readonly IIssues _issues = Substitute.For<IIssues>();
    PendingIssue _payload = null!;
    PendingIssue _filed = null!;
    string _preview = string.Empty;
    string _body = string.Empty;

    async Task Because()
    {
        var options = new PrompterOptions();
        _body = PublishedMarkdown.Neutralize("Ask @someone: [Guide](https://elsewhere.invalid/path)\n\n```cs\n// @someone\n```", options);
        _payload = new("Cratis", "Arc", "Commands", _body, []);
        var pending = new PendingIssues(new ControlledTime(DateTimeOffset.UnixEpoch));
        var scope = new IssueConsent(1, 2, 3);
        var token = await pending.Present(_payload, scope, _ =>
        {
            _preview = IssuePreview.Text(_payload)!;
            return Task.FromResult(4UL);
        });
        _issues.File(Arg.Any<PendingIssue>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _filed = call.Arg<PendingIssue>();
            return Task.FromResult(new FiledIssue(1, "Arc", "https://github.com/Cratis/Arc/issues/1"));
        });
        var confirmation = new IssueConfirmation(_issues, pending, null!, Microsoft.Extensions.Options.Options.Create(options), NullLogger<IssueConfirmation>.Instance);
        await confirmation.Submit(new(IssueButton.FileAction, token), scope, 4, TestContext.Current.CancellationToken);
    }

    [Fact] void should_produce_a_reviewable_preview() => (_preview is not null).ShouldBeTrue();
    [Fact] void should_keep_the_destination_host_visible_in_the_preview() => _preview.Contains("elsewhere\\.invalid", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_file_the_same_immutable_payload() => ReferenceEquals(_filed, _payload).ShouldBeTrue();
    [Fact] void should_keep_the_filed_body_byte_for_byte_equal() => Encoding.UTF8.GetBytes(_filed.Body).ShouldEqual(Encoding.UTF8.GetBytes(_body));
    [Fact] void should_keep_the_safe_label_and_visible_host() => _filed.Body.Contains("Guide `https://elsewhere.invalid/path`", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_the_code_example() => _filed.Body.Contains("```cs\n// @someone\n```", StringComparison.Ordinal).ShouldBeTrue();
}
