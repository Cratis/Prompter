// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Builds Npgsql connection strings for the in-cluster database.
/// </summary>
public static class PostgresConnection
{
    /// <summary>
    /// Builds the connection string for a role.
    /// </summary>
    /// <param name="user">The role to connect as.</param>
    /// <param name="password">The role's password, or a Kubernetes variable reference.</param>
    /// <returns>The connection string.</returns>
    public static string For(string user, string password) =>
        $"Host={PostgresDeployment.Name};Port={PostgresDeployment.Port};Database={PostgresDeployment.DatabaseName};Username={user};Password={password}";
}
