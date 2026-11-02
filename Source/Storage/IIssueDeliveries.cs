// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Storage;

/// <summary>Claims at most one processing attempt for a signed opened issue.</summary>
public interface IIssueDeliveries
{
    /// <summary>Atomically claims an issue; a successful claim is never automatically retried.</summary>
    /// <param name="repository">The signed repository full name.</param>
    /// <param name="number">The positive issue number.</param>
    /// <param name="cancellationToken">The claim deadline.</param>
    /// <returns>Whether this caller owns the first attempt.</returns>
    Task<bool> Claim(string repository, int number, CancellationToken cancellationToken);
}
