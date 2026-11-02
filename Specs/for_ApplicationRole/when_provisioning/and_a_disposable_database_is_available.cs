// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.for_ApplicationRole.given;
using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_ApplicationRole.when_provisioning;

public class and_a_disposable_database_is_available : a_provisioned_application_role
{
    readonly Dictionary<string, string?> _refusals = [];
    bool _roundTrip;
    bool _claimed;
    bool[] _attributes = [];
    long _memberships;
    Exception? _schemaCheck;

    async Task Because()
    {
        if (!HasDatabase) return;
        _schemaCheck = await Catch.Exception(() => new Chunks(App).EnsureSchema());

        var chunks = new Chunks(App);
        var chunk = new Chunk(new ChunkId("spec"), new PageUrl("https://example.test/page"), "Title", "Heading", "Content", new ContentHash("hash"));
        await chunks.Upsert(chunk, new float[1024]);
        var hashes = await chunks.GetHashes();
        await chunks.RemoveAllExcept(new HashSet<ChunkId>());
        _roundTrip = hashes.ContainsKey(chunk.Id);
        _claimed = await new IssueDeliveries(App).Claim("Cratis/Arc", 1, default);

        await Refuse("create role", "CREATE ROLE spec_forbidden");
        await Refuse("create database", "CREATE DATABASE spec_forbidden");
        await Refuse("create table", $"CREATE TABLE {Schema}.spec_forbidden(i int)");
        await Refuse("run program", "COPY (SELECT 1) TO PROGRAM 'true'");
        await Refuse("write history", "INSERT INTO schema_migrations(version) VALUES ('9.9.9')");
        try
        {
            await using var other = Source(Role, "spec-placeholder", "template1");
            await using var connection = await other.OpenConnectionAsync();
        }
        catch (PostgresException error)
        {
            _refusals["other database"] = error.SqlState;
        }

        await using var command = Admin.CreateCommand(
            "SELECT rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls, (SELECT count(*) FROM pg_auth_members WHERE member = pg_roles.oid) FROM pg_roles WHERE rolname = $1");
        command.Parameters.AddWithValue(Role);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        _attributes = [.. Enumerable.Range(0, 5).Select(reader.GetBoolean)];
        _memberships = reader.GetInt64(5);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_ensure_a_current_schema_without_ddl()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        (_schemaCheck is null).ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_read_and_write_the_stores()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _roundTrip.ShouldBeTrue();
        _claimed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_refuse_everything_else_for_lack_of_privilege()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _refusals.Count.ShouldEqual(6);
        _refusals.Values.All(state => state == "42501").ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_hold_no_elevated_attribute_or_membership()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _attributes.Any(value => value).ShouldBeFalse();
        _memberships.ShouldEqual(0L);
    }

    async Task Refuse(string name, string sql)
    {
        try
        {
            await using var command = App.CreateCommand(sql);
            await command.ExecuteNonQueryAsync();
            _refusals[name] = null;
        }
        catch (PostgresException error)
        {
            _refusals[name] = error.SqlState;
        }
    }
}
