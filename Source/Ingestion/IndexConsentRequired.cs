// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// A full rebuild was requested without explicit manual consent.
/// </summary>
public class IndexConsentRequired() : Exception("Full indexing requires manual confirmation: FULL REINDEX. Scheduled full runs are forbidden.");
