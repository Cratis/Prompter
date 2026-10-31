// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Default sink; routing has no telemetry side effects.</summary>
public sealed class NoOpClassificationSink : IClassificationSink
{
    /// <inheritdoc/>
    public void Record(ClassificationObservation observation)
    {
    }
}
