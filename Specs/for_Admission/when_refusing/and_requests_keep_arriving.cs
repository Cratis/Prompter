// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Admission.when_refusing;

public class and_requests_keep_arriving : given.an_admission
{
    bool _first;
    bool _sameUser;
    bool _sameChannel;
    bool _expired;

    void Because()
    {
        _first = Guard.MayReply(new(123, 1, 1));
        _sameUser = Guard.MayReply(new(123, 2, 1));
        _sameChannel = Guard.MayReply(new(123, 1, 2));
        Clock.Advance(TimeSpan.FromMinutes(10));
        _expired = Guard.MayReply(new(123, 1, 1));
    }

    [Fact] void should_allow_one_reply() => _first.ShouldBeTrue();
    [Fact] void should_silence_a_cross_channel_user_flood() => _sameUser.ShouldBeFalse();
    [Fact] void should_silence_a_many_user_channel_flood() => _sameChannel.ShouldBeFalse();
    [Fact] void should_allow_a_reply_after_the_cooldown() => _expired.ShouldBeTrue();
}
