// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Provisions the least-privilege database role the application runs as: it can read and write the tables
/// of the current schema and nothing else.
/// </summary>
public static class ApplicationRole
{
    /// <summary>
    /// A fixed, arbitrary key ("ROLE" in ASCII) serializing provisioning across concurrent runs.
    /// </summary>
    const long ProvisionLockKey = 0x524F4C45;

    /// <summary>
    /// Creates or updates the role and its grants; running it again leaves the same result.
    /// </summary>
    /// <remarks>
    /// Every statement is produced by the server with <c language="csharp">format</c> using <c language="csharp">%I</c> and
    /// <c language="csharp">%L</c>, so names and the password are never concatenated into SQL on the client. It must run as a
    /// role that may create roles and grant on the schema (the bootstrap role).
    /// </remarks>
    /// <param name="admin">The data source connected as the privileged role.</param>
    /// <param name="role">The role to provision.</param>
    /// <param name="password">The password for the role.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the role is provisioned.</returns>
    public static async Task Provision(NpgsqlDataSource admin, DatabaseRoleName role, string password, CancellationToken cancellationToken = default)
    {
        await using var connection = await admin.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Keep the password out of the server log, and serialize concurrent provisioning runs.
        await Run(connection, "SET LOCAL log_statement = 'none'", cancellationToken);
        await Run(connection, "SET LOCAL log_min_error_statement = 'panic'", cancellationToken);
        await Run(connection, $"SELECT pg_advisory_xact_lock({ProvisionLockKey})", cancellationToken);

        var exists = await Scalar<bool>(connection, "SELECT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = $1)", cancellationToken, role.Value);
        if (!exists)
        {
            await Formatted(connection, "CREATE ROLE %1$I", cancellationToken, role.Value);
        }

        await Formatted(
            connection,
            "ALTER ROLE %1$I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS NOINHERIT PASSWORD %2$L",
            cancellationToken,
            role.Value,
            password);

        var schema = await Scalar<string>(connection, "SELECT current_schema()", cancellationToken);
        var database = await Scalar<string>(connection, "SELECT current_database()", cancellationToken);

        await Formatted(connection, "ALTER ROLE %1$I SET search_path = %2$I", cancellationToken, role.Value, schema);
        await Formatted(connection, "REVOKE CREATE ON SCHEMA %1$I FROM PUBLIC", cancellationToken, schema);
        await Formatted(connection, "GRANT USAGE ON SCHEMA %1$I TO %2$I", cancellationToken, schema, role.Value);
        await Formatted(connection, "GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %1$I TO %2$I", cancellationToken, schema, role.Value);

        // The migration history is written only by the migrating role.
        await Formatted(connection, "REVOKE INSERT, UPDATE, DELETE ON %1$I.schema_migrations FROM %2$I", cancellationToken, schema, role.Value);
        await Formatted(connection, "GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %1$I TO %2$I", cancellationToken, schema, role.Value);
        await Formatted(connection, "GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA %1$I TO %2$I", cancellationToken, schema, role.Value);
        await Formatted(connection, "ALTER DEFAULT PRIVILEGES IN SCHEMA %1$I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %2$I", cancellationToken, schema, role.Value);
        await Formatted(connection, "ALTER DEFAULT PRIVILEGES IN SCHEMA %1$I GRANT USAGE, SELECT ON SEQUENCES TO %2$I", cancellationToken, schema, role.Value);

        await Formatted(connection, "GRANT CONNECT ON DATABASE %1$I TO %2$I", cancellationToken, database, role.Value);
        await Formatted(connection, "REVOKE TEMPORARY ON DATABASE %1$I FROM PUBLIC", cancellationToken, database);
        var others = await Column(connection, "SELECT datname FROM pg_database WHERE datallowconn AND datname <> current_database()", cancellationToken);
        foreach (var other in others)
        {
            await Formatted(connection, "REVOKE CONNECT ON DATABASE %1$I FROM PUBLIC", cancellationToken, other);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    static async Task Formatted(NpgsqlConnection connection, string template, CancellationToken cancellationToken, params string[] arguments)
    {
        string statement;
        await using (var format = new NpgsqlCommand("SELECT format($1::text, VARIADIC $2::text[])", connection))
        {
            format.Parameters.AddWithValue(template);
            format.Parameters.AddWithValue(arguments);
            statement = (string)(await format.ExecuteScalarAsync(cancellationToken))!;
        }

        await Run(connection, statement, cancellationToken);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Statements are constants or were produced by the server with format(%I, %L); no caller input is concatenated.")]
    static async Task Run(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Statements are constants or were produced by the server with format(%I, %L); no caller input is concatenated.")]
    static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql, CancellationToken cancellationToken, params string[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter);
        }

        return (T)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Statements are constants or were produced by the server with format(%I, %L); no caller input is concatenated.")]
    static async Task<IReadOnlyList<string>> Column(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        var values = new List<string>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }
}
