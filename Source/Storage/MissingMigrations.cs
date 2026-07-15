// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The exception that is thrown when no embedded migration resources can be discovered in the assembly,
/// which indicates a broken build (the migration resources were not embedded).
/// </summary>
/// <param name="resourcePrefix">The resource-name prefix that was searched.</param>
public class MissingMigrations(string resourcePrefix)
    : Exception($"No embedded migration resources were found under '{resourcePrefix}'");
