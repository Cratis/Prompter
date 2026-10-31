// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_GuidanceButtons.when_rejecting;

public class and_answering_fails : for_AskPrompter.given.an_interaction
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_suppress_mentions_in_deferred_refusal_and_error_replies(bool refused)
    {
        Options.Discord.RateLimitedReply = "@everyone limited";
        Options.Discord.ErrorReply = "@everyone failed";
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Answer>(refused ? new WorkRefused() : new IncompleteCrawl()));
        Button("Which product?\n\n> How do I handle commands?", customId: "clarify:arc");
        await Buttons.Clarify();
        Payloads.Count.ShouldEqual(2);
        using var payload = JsonDocument.Parse(Payloads[1]);
        var message = payload.RootElement;
        message.GetProperty("content").GetString().ShouldEqual(refused ? Options.Discord.RateLimitedReply : Options.Discord.ErrorReply);
        message.GetProperty("flags").GetInt32().ShouldEqual(64);
        message.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength().ShouldEqual(0);
    }
}
