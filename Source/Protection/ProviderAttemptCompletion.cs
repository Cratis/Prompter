// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Durable completion evidence, absent a fully known cost.</summary>
/// <param name="Cost">Known USD usage, or null to retain funding.</param>
internal sealed record ProviderAttemptCompletion(decimal? Cost);
