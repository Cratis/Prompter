// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_a_body_read_is_canceled : given.a_docs_fetch
{
    readonly CancellationTokenSource _cancellation = new();
    StalledStream _stream = null!;
    bool _canceled;

    void Establish()
    {
        _stream = new StalledStream(_cancellation);
        Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(_stream) };
    }

    async Task Because()
    {
        await Crawl(_cancellation.Token);
        _canceled = Error is OperationCanceledException;
    }

    void Destroy()
    {
        _stream.Dispose();
        _cancellation.Dispose();
    }

    [Fact] void should_propagate_caller_cancellation() => _canceled.ShouldBeTrue();
    [Fact] void should_not_publish_any_page() => Pages.ShouldBeEmpty();
    [Fact] void should_dispose_the_stalled_body() => _stream.Disposed.ShouldBeTrue();

    sealed class StalledStream(CancellationTokenSource cancellation) : MemoryStream
    {
        public bool Disposed { get; private set; }
        public override bool CanSeek => false;

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await cancellation.CancelAsync();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
