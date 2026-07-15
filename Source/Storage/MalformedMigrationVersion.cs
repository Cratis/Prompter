// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The exception that is thrown when a migration version cannot be parsed from its textual form.
/// </summary>
/// <param name="value">The value that could not be parsed as a version.</param>
public class MalformedMigrationVersion(string value)
    : Exception($"'{value}' is not a valid migration version");
