// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NetCord.JsonModels;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_the_replied_message_is_unknown : given.an_admitted_message
{
    void Establish()
    {
        var json = Json(100, 5, 3, "what about version 2?");
        json.Type = MessageType.Reply;
        json.MessageReference = new JsonMessageReference { Type = MessageReferenceType.Reply, MessageId = 50, ChannelId = 5 };
        json.ReferencedMessage = null;
        Message = new(json, null, null, Rest);
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
