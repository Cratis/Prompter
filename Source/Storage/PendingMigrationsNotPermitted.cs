// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The exception that is thrown when migrations are pending but the connected database role may not apply them.
/// </summary>
/// <param name="versions">The versions that are pending.</param>
/// <param name="inner">The database failure that refused the change.</param>
public class PendingMigrationsNotPermitted(IEnumerable<MigrationVersion> versions, Exception inner)
    : Exception(
        $"Migrations {string.Join(", ", versions)} are pending and this database role may not apply them; run the 'migrate' command with the migration role first",
        inner);
