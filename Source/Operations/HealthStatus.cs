// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Operations;

/// <summary>
/// The payload returned by <c language="csharp">GET /healthz</c>.
/// </summary>
/// <param name="Status">A short overall verdict: <c language="csharp">healthy</c> or <c language="csharp">unhealthy</c>.</param>
/// <param name="Database">Whether the database was reachable.</param>
/// <param name="Gateway">Whether the Discord gateway is connected.</param>
internal sealed record HealthStatus(string Status, bool Database, bool Gateway);
