// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>The bounded inner transport request matched to one durable permit.</summary>
/// <param name="Provider">The fixed provider label.</param>
/// <param name="Model">The verified model.</param>
/// <param name="Upper">Conservative USD exposure.</param>
/// <param name="InputCount">The batch size.</param>
internal sealed record ProviderAttemptRequest(string Provider, string Model, decimal Upper, int InputCount = 1);
