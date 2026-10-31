// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_the_question_was_deleted : given.a_forward
{
    void Establish() => Discord.Author = null;

    async Task Because() => Reply = await Forwards.Forward(Documented, TestContext.Current.CancellationToken);

    [Fact] void should_say_it_is_gone() => Reply.ShouldEqual(MaintainerForwards.QuestionGoneReply);
    [Fact] void should_not_post() => Channel.DidNotReceive().Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
