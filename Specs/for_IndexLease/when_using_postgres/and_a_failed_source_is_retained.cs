// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_IndexLease.when_using_postgres;

[Trait("Category", "Integration")]
[Collection("IndexPostgres")]
public class and_a_failed_source_is_retained : given.a_database
{
    readonly Dictionary<string, (DateTime IndexedAt, string Version)> _pages = [];
    Exception? _mismatch;
    int _removed;

    async Task Establish()
    {
        await using var seed = Database.CreateCommand(
            """
            INSERT INTO documentation_pages (page_url, indexed_at, product_version) VALUES
                ('https://cratis.io/arc/', '2020-01-01T00:00:00Z', 'v0'),
                ('https://github.com/Cratis/Arc/releases/tag/v0', '2020-01-01T00:00:00Z', 'v0'),
                ('https://cratis.io/deleted/', '2020-01-01T00:00:00Z', 'v0');
            """);
        await seed.ExecuteNonQueryAsync();
    }

    async Task Because()
    {
        await using var lease = await IndexLease.Acquire(Database, CancellationToken.None);
        await lease.CheckProfile("voyage-4/1024/markdown-v1", false, CancellationToken.None);
        var snapshot = new IndexSnapshot([], new HashSet<ChunkId> { new("keep") }, [new(new("https://cratis.io/arc/"), null, "v1")])
        {
            PreservedSources = ["https://github.com/Cratis/Arc/releases/"]
        };
        _removed = await lease.CommitIndex(snapshot, CancellationToken.None);
        await lease.CheckProfile("different-model/1024/markdown-v1", true, CancellationToken.None);
        _mismatch = await Catch.Exception(() => lease.CommitIndex(snapshot, CancellationToken.None));
        await using var command = Database.CreateCommand("SELECT page_url, indexed_at, product_version FROM documentation_pages");
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) _pages.Add(reader.GetString(0), (reader.GetDateTime(1), reader.GetString(2)));
    }

    [Fact] void should_remove_stale_chunks_from_healthy_sources() => _removed.ShouldEqual(1);
    [Fact] void should_prune_stale_metadata_from_healthy_sources() => _pages.ContainsKey("https://cratis.io/deleted/").ShouldBeFalse();
    [Fact] void should_refresh_healthy_metadata() => _pages["https://cratis.io/arc/"].Version.ShouldEqual("v1");
    [Fact] void should_retain_failed_source_metadata() => _pages["https://github.com/Cratis/Arc/releases/tag/v0"].Version.ShouldEqual("v0");
    [Fact] void should_not_stamp_failed_source_as_fresh() => _pages["https://github.com/Cratis/Arc/releases/tag/v0"].IndexedAt.ShouldEqual(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    [Fact] void should_not_mix_embedding_profiles_with_retained_chunks() => _mismatch.ShouldBeOfExactType<IndexProfileMismatch>();
}
