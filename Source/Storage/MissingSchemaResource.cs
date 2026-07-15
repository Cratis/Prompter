// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// The exception that is thrown when the embedded database schema resource cannot be found.
/// </summary>
/// <param name="resourceName">The name of the resource that was not found.</param>
public class MissingSchemaResource(string resourceName)
    : Exception($"Embedded schema resource '{resourceName}' was not found in the assembly");
