// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Protection;

/// <summary>The in-memory mirror of the maintainer kill switch. Pausing behaves like a runtime AnsweringEnabled set to false.</summary>
/// <param name="logger">The logger; transitions are logged without any identity.</param>
public sealed class AnsweringSwitch(ILogger<AnsweringSwitch> logger)
{
    volatile bool _paused;

    /// <summary>Gets a value indicating whether answering is paused.</summary>
    public bool Paused => _paused;

    /// <summary>Applies the pause state, recording only transitions.</summary>
    /// <param name="paused">Whether answering should be paused.</param>
    public void Apply(bool paused)
    {
        if (_paused == paused)
        {
            return;
        }

        _paused = paused;
        if (paused)
        {
            logger.AnsweringPaused();
        }
        else
        {
            logger.AnsweringResumed();
        }

        ProviderMetrics.Event("protection", paused ? "paused" : "resumed");
    }
}
