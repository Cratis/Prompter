// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The exception that is thrown when a database role name is not a plain lowercase identifier.
/// </summary>
/// <param name="value">The rejected value.</param>
public class InvalidDatabaseRoleName(string value)
    : Exception($"'{value}' is not a valid database role name; use 1-63 characters of a-z, 0-9 and _, not starting with a digit");
