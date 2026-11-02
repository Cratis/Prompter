// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_response_bodies_exceed_the_limits : given.a_docs_fetch
{
    readonly List<Exception?> _errors = [];
    readonly List<int> _reads = [];
    readonly List<bool> _disposed = [];

    void Establish()
    {
        Options.Ingestion.MaxSitemapBytes = 65536;
        Options.Ingestion.Sites.MaxPageBytes = 4096;
    }

    async Task Because()
    {
        foreach (var sitemap in new[] { false, true })
        {
            foreach (var declared in new[] { false, true })
            {
                await using var stream = new ObservedStream(new byte[(sitemap ? 65536 : 4096) + 1]);
                Respond = uri =>
                {
                    if (!sitemap && uri.AbsolutePath == "/sitemap-0.xml") return Text("<loc>https://docs.invalid/a</loc>");
                    var content = new StreamContent(stream);
                    if (declared) content.Headers.ContentLength = stream.Length;
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                };
                await Crawl();
                _errors.Add(Error);
                _reads.Add(stream.BytesRead);
                _disposed.Add(stream.Disposed);
            }
        }
    }

    [Fact] void should_reject_declared_and_streaming_overflows() => _errors.TrueForAll(error => error is IncompleteCrawl).ShouldBeTrue();
    [Fact] void should_stop_streaming_at_the_bound() => _reads.ShouldEqual([4097, 0, 65537, 0]);
    [Fact] void should_dispose_every_response_body() => _disposed.TrueForAll(value => value).ShouldBeTrue();

    sealed class ObservedStream(byte[] data) : MemoryStream(data)
    {
        public int BytesRead { get; private set; }
        public bool Disposed { get; private set; }
        public override bool CanSeek => false;

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var count = Read(buffer.Span);
            BytesRead += count;
            return ValueTask.FromResult(count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
