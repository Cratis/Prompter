// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Prompter.Eval;

/// <summary>Loads only eval environment variables, without constructing external clients.</summary>
internal static class EvalHost
{
    /// <summary>Reads the strict eval prefix without default host configuration sources.</summary>
    /// <returns>Eval-only options.</returns>
    public static EvalConfiguration Configuration() => new ConfigurationBuilder()
        .AddEnvironmentVariables(prefix: "Cratis__PrompterEval__")
        .Build()
        .Get<EvalConfiguration>() ?? new();
}
