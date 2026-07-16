// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_UserHash.when_hashing;

public class and_the_key_differs : Specification
{
    string _first = null!;
    string _second = null!;

    void Because()
    {
        _first = UserHash.For(123456789012345678, "one-secret-key");
        _second = UserHash.For(123456789012345678, "another-secret-key");
    }

    [Fact] void should_produce_different_hashes() => (_first != _second).ShouldBeTrue();
}
