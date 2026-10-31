// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_the_request_cannot_proceed : given.a_hand_off
{
    string _withoutRole = null!;
    string _wrongAction = null!;
    string _noQuestionAsker = null!;

    async Task Because()
    {
        _withoutRole = await HandOffs.Request(Click, CancellationToken.None);
        Options.Discord.MaintainerRoleId = 9;
        _wrongAction = await HandOffs.Request(Click with { CustomId = AnswerActionButton.CustomId(AnswerActionButton.DocumentedPrefix, new(2, 4)) }, CancellationToken.None);
        _noQuestionAsker = await HandOffs.Request(Click with { Actor = new(1, 5, 6), CustomId = AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, new(5, 0)) }, CancellationToken.None);
    }

    [Fact] void should_not_be_configured_without_a_role() => _withoutRole.ShouldEqual(HumanHandOffs.NotConfiguredReply);
    [Fact] void should_reject_another_action() => _wrongAction.ShouldEqual(HumanHandOffs.UnavailableReply);
    [Fact] void should_not_find_a_command_asker() => _noQuestionAsker.ShouldEqual(HumanHandOffs.QuestionGoneReply);
}
