// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Discovers the SQL migrations embedded in an assembly. Migration files live under
/// <c>Storage/Migrations</c> and are embedded with manifest names such as
/// <c>Cratis.Prompter.Storage.Migrations.v1_0_0.sql</c>; the version is parsed from the file portion.
/// </summary>
public static class EmbeddedMigrations
{
    /// <summary>
    /// The manifest resource-name prefix shared by every embedded migration.
    /// </summary>
    public const string ResourcePrefix = "Cratis.Prompter.Storage.Migrations.";

    /// <summary>
    /// The manifest resource-name suffix shared by every embedded migration.
    /// </summary>
    public const string ResourceSuffix = ".sql";

    /// <summary>
    /// Loads every embedded migration from the given assembly, unordered. The returned SQL and version come
    /// straight from the embedded resources; ordering is left to <see cref="MigrationPlan.Pending"/>.
    /// </summary>
    /// <param name="assembly">The assembly whose embedded migration resources are read.</param>
    /// <returns>The discovered migrations, in no particular order.</returns>
    /// <exception cref="MissingMigrations">Thrown when no embedded migration resources are found.</exception>
    public static IReadOnlyList<Migration> LoadFrom(Assembly assembly)
    {
        var migrations = new List<Migration>();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resourceName.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            var versionToken = resourceName[ResourcePrefix.Length..^ResourceSuffix.Length];
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new MissingMigrations(ResourcePrefix);
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            migrations.Add(new Migration(MigrationVersion.Parse(versionToken), resourceName, sql));
        }

        if (migrations.Count == 0)
        {
            throw new MissingMigrations(ResourcePrefix);
        }

        return migrations;
    }
}
