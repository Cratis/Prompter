// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_ContentHash.when_hashing;

public class and_content_is_identical : Specification
{
    ContentHash _first = null!;
    ContentHash _second = null!;

    void Because()
    {
        _first = ContentHash.For("The same content");
        _second = ContentHash.For("The same content");
    }

    [Fact] void should_produce_equal_hashes() => _first.ShouldEqual(_second);
}
