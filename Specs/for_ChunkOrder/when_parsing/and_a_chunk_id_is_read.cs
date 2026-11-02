// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_ChunkOrder.when_parsing;

public class and_a_chunk_id_is_read : Specification
{
    [Theory]
    [InlineData("https://cratis.io/a/#summary", -1, 0)]
    [InlineData("https://cratis.io/a/#5", 5, 0)]
    [InlineData("https://cratis.io/a/#5.2", 5, 2)]
    [InlineData("https://cratis.io/a/#x", int.MaxValue, 0)]
    [InlineData("https://cratis.io/a/#5.x", int.MaxValue, 0)]
    [InlineData("https://cratis.io/a/#1.2.3", int.MaxValue, 0)]
    [InlineData("no-separator", int.MaxValue, 0)]
    [InlineData(null, int.MaxValue, 0)]
    public void should_parse_the_order(string? id, int index, int piece) => ChunkOrder.Of(id).ShouldEqual((index, piece));
}
