// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// An index run has exceeded its configured token, dollar or daily scheduled allowance, or lacks pricing.
/// </summary>
public class IndexBudgetExceeded() : Exception("Index budget unavailable or exceeded; no additional embeddings were dispatched.");
