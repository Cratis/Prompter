// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_pages_state_provenance : given.an_indexer
{
    IReadOnlyCollection<DocumentationMetadata> _published = null!;

    void Establish()
    {
        Indexer = Create(new FakeDocsSite([
            new DocsPage(new PageUrl("https://cratis.io/a.md"), "---\nlastUpdated: 2026-03-04\nversion: 1.2.3\n---\n# A\n\nAlpha."),
            new DocsPage(new PageUrl("https://cratis.io/b.md"), "# B\n\nBeta.")
        ]));
        Lease.CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Do<IReadOnlyCollection<DocumentationMetadata>>(pages => _published = pages), Arg.Any<CancellationToken>()).Returns(0);
    }

    async Task Because() => await Indexer.Run();

    [Fact] void should_publish_metadata_for_every_crawled_page() => _published.Count.ShouldEqual(2);
    [Fact] void should_carry_the_stated_version() => _published.Single(page => page.Page.Value.EndsWith("a.md", StringComparison.Ordinal)).ProductVersion.ShouldEqual("1.2.3");
    [Fact] void should_leave_unstated_values_unknown() => _published.Single(page => page.Page.Value.EndsWith("b.md", StringComparison.Ordinal)).PageLastModified.ShouldBeNull();
}
