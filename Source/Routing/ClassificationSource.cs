// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>The source of advisory data.</summary>
public enum ClassificationSource
{
    /// <summary>Local safe baseline.</summary>
    Deterministic = 0,

    /// <summary>Validated private Decision API response.</summary>
    Laya = 1
}
