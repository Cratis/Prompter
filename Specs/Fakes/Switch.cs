// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>Creates kill switches for specs that do not exercise pausing.</summary>
public static class Switch
{
    /// <summary>Creates a switch that is not paused.</summary>
    /// <returns>A new, running switch.</returns>
    public static AnsweringSwitch Running()
    {
        var answering = new AnsweringSwitch(NullLogger<AnsweringSwitch>.Instance);
        answering.Refresh(false, answering.Version);
        return answering;
    }
}
