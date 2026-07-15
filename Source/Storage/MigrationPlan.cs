// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Holds the pure planning logic for database migrations: given the migrations available in the assembly
/// and the versions already recorded as applied, it decides which migrations are still pending and orders
/// them for application. It performs no I/O and touches no database.
/// </summary>
public static class MigrationPlan
{
    /// <summary>
    /// Determines the migrations that still need to be applied, ordered by ascending version. A migration is
    /// pending when its version is not present in <paramref name="appliedVersions"/>.
    /// </summary>
    /// <param name="available">All migrations discovered in the assembly, in any order.</param>
    /// <param name="appliedVersions">The versions already recorded in the tracking table.</param>
    /// <returns>The pending migrations in the exact order they must be applied.</returns>
    public static IReadOnlyList<Migration> Pending(
        IEnumerable<Migration> available,
        IReadOnlySet<MigrationVersion> appliedVersions) =>
        available
            .Where(migration => !appliedVersions.Contains(migration.Version))
            .OrderBy(migration => (migration.Version.Major, migration.Version.Minor, migration.Version.Patch))
            .ToArray();
}
