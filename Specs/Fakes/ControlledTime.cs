// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.Fakes;

/// <summary>
/// A clock the specs move by hand, so expiry can be observed without waiting for it.
/// </summary>
public sealed class ControlledTime(DateTimeOffset now) : TimeProvider
{
    DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
