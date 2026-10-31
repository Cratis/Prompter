// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.AspNetCore.Http;

namespace Cratis.Prompter.Specs.for_BoundedBody.when_reading;

public class and_the_body_is_oversized : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_reject_declared_and_chunked_oversized_bodies(bool declared)
    {
        var request = new DefaultHttpContext().Request;
        await using var body = new MemoryStream(new byte[101]);
        request.Body = body;
        request.ContentLength = declared ? 101 : null;
        var result = await BoundedBody.Read(request, 100, CancellationToken.None);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task should_accept_the_exact_boundary()
    {
        var request = new DefaultHttpContext().Request;
        await using var body = new MemoryStream(new byte[100]);
        request.Body = body;
        var result = await BoundedBody.Read(request, 100, CancellationToken.None);
        result!.Length.ShouldEqual(100);
    }
}
