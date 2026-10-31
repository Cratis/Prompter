// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_SafeMessages.when_building;

public class a_message_from_untrusted_text : Specification
{
    const string Hostile = "@everyone @here <@&9> <@3>";

    static bool Nobody(AllowedMentionsProperties mentions) =>
        !mentions.Everyone && !(mentions.AllowedRoles?.Any() ?? false) && !(mentions.AllowedUsers?.Any() ?? false);

    [Fact] void should_keep_the_text() => SafeMessages.Message(Hostile).Content.ShouldEqual(Hostile);
    [Fact] void should_allow_no_mentions_in_a_message() => Nobody(SafeMessages.Message(Hostile).AllowedMentions!).ShouldBeTrue();
    [Fact] void should_allow_no_mentions_in_a_followup() => Nobody(SafeMessages.Followup(Hostile).AllowedMentions!).ShouldBeTrue();
    [Fact] void should_allow_no_mentions_in_a_reply() => Nobody(SafeMessages.Reply(Hostile).AllowedMentions!).ShouldBeTrue();
    [Fact] void should_not_notify_a_third_party_author() => SafeMessages.Reply(Hostile, mentionAsker: false).AllowedMentions!.ReplyMention.ShouldBeFalse();
    [Fact] void should_still_notify_the_asker_the_reply_references() => SafeMessages.Reply(Hostile).AllowedMentions!.ReplyMention.ShouldBeTrue();
}
