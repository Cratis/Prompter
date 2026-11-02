// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Deadline-aware transport operations used by a bounded feedback lifecycle.</summary>
/// <param name="Acknowledge">Acknowledges a new or malformed click.</param>
/// <param name="Repeat">Silently acknowledges an already-recorded verdict.</param>
/// <param name="Refuse">Sends a suppressed refusal if the reply budget permits.</param>
public sealed record FeedbackCallbacks(
    Func<CancellationToken, Task> Acknowledge,
    Func<CancellationToken, Task> Repeat,
    Func<CancellationToken, Task> Refuse);
