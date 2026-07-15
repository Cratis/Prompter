// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents the parsed semantic version of a database migration. Versions order the migrations and
/// identify which ones have already been applied in the <c>schema_migrations</c> tracking table.
/// </summary>
/// <param name="Major">The major version component.</param>
/// <param name="Minor">The minor version component.</param>
/// <param name="Patch">The patch version component.</param>
public record MigrationVersion(int Major, int Minor, int Patch)
{
    /// <summary>
    /// Parses a version from its textual form. Both the migration file form (<c>v1_2_0</c>) and the dotted
    /// canonical form (<c>1.2.0</c>) are accepted; a leading <c>v</c> is optional, components are separated
    /// by either <c>.</c> or <c>_</c>, and any omitted component defaults to zero.
    /// </summary>
    /// <param name="value">The version text to parse.</param>
    /// <returns>The parsed <see cref="MigrationVersion"/>.</returns>
    /// <exception cref="MalformedMigrationVersion">Thrown when <paramref name="value"/> is not a valid version.</exception>
    public static MigrationVersion Parse(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }

        var parts = trimmed.Split(['.', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is 0 or > 3)
        {
            throw new MalformedMigrationVersion(value);
        }

        var components = new int[3];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out components[index]))
            {
                throw new MalformedMigrationVersion(value);
            }
        }

        return new(components[0], components[1], components[2]);
    }

    /// <summary>
    /// Gets the canonical dotted text form of the version, e.g. <c>1.2.0</c>. This is the exact form
    /// stored in the <c>schema_migrations</c> tracking table.
    /// </summary>
    /// <returns>The canonical version text.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}");
}
