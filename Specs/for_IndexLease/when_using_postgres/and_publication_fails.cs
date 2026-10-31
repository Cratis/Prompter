// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_IndexLease.when_using_postgres;

[Trait("Category", "Integration")]
[Collection("IndexPostgres")]
public class and_publication_fails : given.a_database
{
    Exception? _error;
    Exception? _mismatch;
    long _remaining;
    string? _profile;

    async Task Because()
    {
        await using var lease = await IndexLease.Acquire(Database, CancellationToken.None);
        _mismatch = await Catch.Exception(async () => await lease.CheckProfile("new-model/1024/markdown-v1", false, CancellationToken.None));
        await lease.CheckProfile("new-model/1024/markdown-v1", true, CancellationToken.None);
        await using (var setup = Database.CreateCommand(
            """
            CREATE OR REPLACE FUNCTION reject_index_publication() RETURNS trigger AS $$
            BEGIN RAISE EXCEPTION 'synthetic publication failure'; END;
            $$ LANGUAGE plpgsql;
            CREATE TRIGGER reject_publication BEFORE UPDATE ON index_profile
                FOR EACH ROW EXECUTE FUNCTION reject_index_publication();
            """))
        {
            await setup.ExecuteNonQueryAsync();
        }

        _error = await Catch.Exception(async () => await lease.CommitIndex([], new HashSet<ChunkId> { new("keep") }, [], CancellationToken.None));
        await using var count = Database.CreateCommand("SELECT count(*) FROM chunks");
        _remaining = (long)(await count.ExecuteScalarAsync())!;
        await using var profile = Database.CreateCommand("SELECT profile FROM index_profile");
        _profile = (string?)await profile.ExecuteScalarAsync();
    }

    [Fact] void should_reject_incremental_model_changes() => _mismatch.ShouldBeOfExactType<IndexProfileMismatch>();
    [Fact] void should_propagate_database_failure() => _error.ShouldBeOfExactType<PostgresException>();
    [Fact] void should_roll_back_stale_deletion() => _remaining.ShouldEqual(2);
    [Fact] void should_preserve_the_previous_profile() => _profile.ShouldEqual("voyage-4/1024/markdown-v1");
}
