// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Shadow is opt-in and never changes returned behavior.</summary>
public enum ClassificationMode
{
    /// <summary>Use validated Laya classifications.</summary>
    Active = 0,

    /// <summary>Sample comparisons but return only the baseline.</summary>
    Shadow = 1
}
