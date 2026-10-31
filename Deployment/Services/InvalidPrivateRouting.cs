// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>The private routing configuration is incomplete or not approved.</summary>
public sealed class InvalidPrivateRouting() : Exception("Private routing configuration is not approved.");
