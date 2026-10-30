// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>A metadata-only, synchronous observation boundary.</summary>
public interface IClassificationSink
{
    /// <summary>Records bounded metadata without request or provider text.</summary>
    /// <param name="observation">Closed-enum metadata.</param>
    void Record(ClassificationObservation observation);
}
