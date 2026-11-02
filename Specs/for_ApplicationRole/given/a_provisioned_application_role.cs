// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_ApplicationRole.given;

/// <summary>
/// A migrated ephemeral schema in a disposable Postgres, with the application role provisioned twice
/// and a data source connected as that role.
/// </summary>
public class a_provisioned_application_role : Specification
{
    const string AdminUser = "postgres";

    public static bool HasDatabase => int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT"), out _);

    protected string Schema { get; } = $"role_{Guid.NewGuid():N}";
    protected string Role { get; } = $"prompter_app_{Guid.NewGuid():N}";
    protected NpgsqlDataSource Admin { get; private set; } = null!;
    protected NpgsqlDataSource App { get; private set; } = null!;
    protected int Port { get; private set; }

    protected async Task Establish()
    {
        if (!HasDatabase) return;
        Port = int.Parse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT")!, CultureInfo.InvariantCulture);
        Admin = Source(AdminUser, null);

        await VectorExtension.Ensure(Admin);

        await Execute($"CREATE SCHEMA {Schema}");
        await new Chunks(Admin).EnsureSchema();
        await ApplicationRole.Provision(Admin, new DatabaseRoleName(Role), "spec-placeholder");
        await ApplicationRole.Provision(Admin, new DatabaseRoleName(Role), "spec-placeholder");
        App = Source(Role, "spec-placeholder");
    }

    protected async Task Destroy()
    {
        if (Admin is null) return;
        if (App is not null) await App.DisposeAsync();
        await Execute($"DROP SCHEMA {Schema} CASCADE");
        await Execute($"DROP OWNED BY {Role}");
        await Execute($"DROP ROLE {Role}");
        await Admin.DisposeAsync();
    }

    protected NpgsqlDataSource Source(string user, string? password, string database = "postgres")
    {
        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = Port,
            Username = user,
            Database = database,
            SearchPath = $"{Schema},public"
        };
        if (password is not null) settings.Password = password;
        var builder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
        builder.UseVector();
        return builder.Build();
    }

    protected async Task Execute(string sql)
    {
        await using var command = Admin.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
