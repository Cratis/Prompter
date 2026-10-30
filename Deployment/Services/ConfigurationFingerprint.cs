// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using Pulumi;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>
/// Fingerprints the final consumed secret map without exposing its values in pod annotations.
/// </summary>
public static class ConfigurationFingerprint
{
    /// <summary>
    /// Creates a deterministic SHA-256 fingerprint, retaining secret and unknown input semantics.
    /// </summary>
    /// <param name="data">The final secret map consumed by the workload.</param>
    /// <returns>A secret output containing only the hexadecimal digest.</returns>
    public static Output<string> Create(InputMap<string> data)
    {
        Output<ImmutableDictionary<string, string>> resolved = data;
        return Output.CreateSecret(resolved.Apply(values =>
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                values.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new[] { pair.Key, pair.Value }))))));
    }
}
