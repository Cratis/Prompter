// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents a single database migration: an ordered unit of schema change that is applied exactly once.
/// </summary>
/// <param name="Version">The semantic version that orders this migration and records it as applied.</param>
/// <param name="ResourceName">The manifest resource name the SQL was loaded from.</param>
/// <param name="Sql">The SQL statements that make up the migration.</param>
public record Migration(MigrationVersion Version, string ResourceName, string Sql);
