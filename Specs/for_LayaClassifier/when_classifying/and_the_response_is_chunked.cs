// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Specs.for_LayaClassifier.when_classifying;

public class and_the_response_is_chunked : given.a_classifier
{
    forward_only _stream = null!;

    Task Because()
    {
        _stream = new(new byte[100000]);
        transport.Content = _ => new StreamContent(_stream);
        return Classify();
    }

    [Fact] void should_not_read_more_than_the_limit_plus_one() => _stream.BytesRead.ShouldEqual(65537);
    [Fact] void should_reject_oversized_streams_without_content_length() => result.Fallback.ShouldEqual(ClassificationFallback.InvalidResponse);

    sealed class forward_only(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
        public int BytesRead { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await base.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }
    }
}
