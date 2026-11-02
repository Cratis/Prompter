// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents the name of a database role, restricted to a plain lowercase identifier.
/// </summary>
public record DatabaseRoleName : ConceptAs<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRoleName"/> record.
    /// </summary>
    /// <param name="value">The role name.</param>
    /// <exception cref="InvalidDatabaseRoleName">The value is not a plain lowercase identifier.</exception>
    public DatabaseRoleName(string value)
        : base(Validated(value))
    {
    }

    public static implicit operator string(DatabaseRoleName name) => name.Value;

    static string Validated(string value) =>
        value is { Length: > 0 and <= 63 } && !char.IsAsciiDigit(value[0]) && value.All(IsAllowed)
            ? value
            : throw new InvalidDatabaseRoleName(value ?? string.Empty);

    static bool IsAllowed(char character) => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '_';
}
