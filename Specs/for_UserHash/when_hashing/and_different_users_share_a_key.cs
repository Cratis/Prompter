// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_UserHash.when_hashing;

public class and_different_users_share_a_key : Specification
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = UserHash.For(111111111111111111, "a-shared-secret-key");
        _second = UserHash.For(222222222222222222, "a-shared-secret-key");
    }

    [Fact] void should_produce_different_hashes() => (_first != _second).ShouldBeTrue();
}
