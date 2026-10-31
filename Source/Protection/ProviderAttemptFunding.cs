// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>A committed attempt's upper exposure and ownership.</summary>
/// <param name="Upper">Conservative USD funding.</param>
/// <param name="Prepaid">Whether this funding belongs to the live parent lease.</param>
internal sealed record ProviderAttemptFunding(decimal Upper, bool Prepaid);
