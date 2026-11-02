// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedDrafting.when_drafting;

public class and_the_generated_payload_has_an_unsafe_destination : for_PaidWork.given.a_paid_work
{
    IssueDraft _draft = null!;
    IssueDraft _alreadyLeased = null!;
    string? _preview;

    async Task Because()
    {
        var chat = Substitute.For<IChatClient>();
        var output = JsonSerializer.Serialize(new { title = "Ask @someone", body = "See [Guide](https://elsewhere.invalid)", product = "Arc" });
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, output))));
        var drafting = new ProtectedDrafting(new IssueDrafting(chat), Work, Microsoft.Extensions.Options.Options.Create(Options));
        _draft = (await drafting.Draft("Commands", null))!;
        _preview = IssuePreview.Text(new("Cratis", "Arc", _draft.Title, _draft.Body, []));
        await using var lease = await Work.BeginInteractive(TestContext.Current.CancellationToken);
        lease.Activate();
        _alreadyLeased = (await drafting.Draft("Commands", null, TestContext.Current.CancellationToken))!;
    }

    [Fact] void should_protect_the_already_leased_payload_identically() => _alreadyLeased.ShouldEqual(_draft);
    [Fact] void should_not_reserve_another_lease_for_the_admitted_path() => Ledger.Received(2).Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>());
    [Fact] void should_protect_the_title_before_preview() => _draft.Title.ShouldEqual("Ask \uFF20someone");
    [Fact] void should_protect_the_body_before_preview() => _draft.Body.ShouldEqual("See Guide (https[:]//elsewhere[.]invalid)");
    [Fact] void should_keep_the_preview_reviewable() => (_preview is not null).ShouldBeTrue();
}
