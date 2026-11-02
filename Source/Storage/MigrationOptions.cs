// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Options for the <c language="csharp">migrate</c> command, bound from the <c language="csharp">Cratis:Prompter:Migration</c> configuration section.
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// Gets or sets the name of the application role to provision; empty to only apply migrations.
    /// </summary>
    public string ApplicationRole { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for the application role.
    /// </summary>
    public string ApplicationRolePassword { get; set; } = string.Empty;
}
