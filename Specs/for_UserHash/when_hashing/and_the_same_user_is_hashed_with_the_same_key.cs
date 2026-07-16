// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_UserHash.when_hashing;

public class and_the_same_user_is_hashed_with_the_same_key : Specification
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = UserHash.For(123456789012345678, "a-shared-secret-key");
        _second = UserHash.For(123456789012345678, "a-shared-secret-key");
    }

    [Fact] void should_produce_equal_hashes() => _first.ShouldEqual(_second);
}
