// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>A non-secret paid workload setting is not authorized or usable.</summary>
/// <param name="reason">A content-free configuration error.</param>
public sealed class InvalidAnsweringReadiness(string reason) : Exception(reason);
