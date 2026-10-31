// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Prompter.Answering;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AskPrompter.when_delivering;

public class and_the_selected_author_is_someone_else : given.an_interaction
{
    void Establish()
    {
        Options.Discord.AnswerInThreadChannelIds = [70];
        Answers.For(Arg.Any<Question>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Answer("Answer", [], 0.9, false, []));
        Select();
    }

    async Task Because() => await Command.Handle();

    [Fact] void should_not_create_a_thread_on_the_selected_message() => Paths.Any(path => path.Contains("threads", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_answer_without_pinging_the_selected_author()
    {
        var messages = Payloads.Select(payload => JsonDocument.Parse(payload)).ToArray();
        try
        {
            var reply = messages.Single(message => message.RootElement.TryGetProperty("message_reference", out _)).RootElement;
            var mentions = reply.GetProperty("allowed_mentions");
            (mentions.TryGetProperty("replied_user", out var notify) && notify.GetBoolean()).ShouldBeFalse();
            reply.GetProperty("allowed_mentions").GetProperty("parse").GetArrayLength().ShouldEqual(0);
        }
        finally
        {
            foreach (var message in messages) message.Dispose();
        }
    }
}
