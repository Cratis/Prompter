// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Storage;

/// <summary>Persists only normalized issue identity, not delivery content or requester identity.</summary>
/// <param name="database">The shared database connection pool.</param>
public sealed class IssueDeliveries(NpgsqlDataSource database) : IIssueDeliveries
{
    /// <summary>Checks a bounded repository identity and positive issue number before scheduling or storage.</summary>
    /// <param name="repository">The repository full name.</param>
    /// <param name="number">The issue number.</param>
    /// <returns>Whether the signed identity is usable.</returns>
    public static bool IsValid(string repository, int number)
    {
        if (number <= 0) return false;
        if (string.IsNullOrWhiteSpace(repository)) return false;
        if (repository.Length > 200) return false;
        var segments = repository.Split('/');
        return segments.Length == 2 && segments.All(segment => segment.Length > 0 && segment is not "." and not ".." &&
            segment.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'));
    }

    /// <inheritdoc/>
    public async Task<bool> Claim(string repository, int number, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsValid(repository, number)) return false;
        await using var command = database.CreateCommand("INSERT INTO github_issue_deliveries (repository, issue_number) VALUES ($1, $2) ON CONFLICT DO NOTHING");
        command.Parameters.AddWithValue(repository.ToLowerInvariant());
        command.Parameters.AddWithValue(number);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }
}
