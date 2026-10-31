// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>What an asker consents to forward to the maintainer channel.</summary>
public enum ForwardKind
{
    /// <summary>A question the documentation could not answer.</summary>
    Documentation = 0,

    /// <summary>A capability the asker could not find.</summary>
    FeatureRequest = 1
}
