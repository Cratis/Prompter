// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Describes an explicitly authorized ingestion pass. Scheduled passes are always incremental.
/// </summary>
/// <param name="Full">Whether to regenerate every embedding.</param>
/// <param name="Confirmation">The operator's full-rebuild confirmation.</param>
/// <param name="Scheduled">Whether to consume today's scheduled slot.</param>
public record IndexRequest(bool Full = false, string Confirmation = "", bool Scheduled = false)
{
    /// <summary>
    /// Validates consent before acquiring a lease or performing work.
    /// </summary>
    /// <exception cref="IndexConsentRequired">Full work lacks explicit manual confirmation.</exception>
    public void Validate()
    {
        var invalidFull = Scheduled || Confirmation != "FULL REINDEX";
        if (Full && invalidFull)
        {
            throw new IndexConsentRequired();
        }
    }
}
