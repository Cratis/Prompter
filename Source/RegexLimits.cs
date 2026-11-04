// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>Limits shared by every regular expression that runs over user or model text.</summary>
public static class RegexLimits
{
    /// <summary>The match timeout in milliseconds; generous enough to hold under load while still bounding a runaway match.</summary>
    public const int TimeoutMilliseconds = 1000;
}
