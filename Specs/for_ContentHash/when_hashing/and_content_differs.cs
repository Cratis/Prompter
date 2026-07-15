// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_ContentHash.when_hashing;

public class and_content_differs : Specification
{
    ContentHash _first = null!;
    ContentHash _second = null!;

    void Because()
    {
        _first = ContentHash.For("Some content");
        _second = ContentHash.For("Different content");
    }

    [Fact] void should_produce_different_hashes() => (_first != _second).ShouldBeTrue();
}
