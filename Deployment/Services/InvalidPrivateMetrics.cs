// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>The private collector configuration is incomplete or not approved.</summary>
public sealed class InvalidPrivateMetrics() : Exception("Private metrics configuration is not approved.");
