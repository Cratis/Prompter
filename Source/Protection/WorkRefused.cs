// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Costly work could not safely be admitted. Contains no underlying connection/provider details.</summary>
public sealed class WorkRefused() : Exception("Prompter is temporarily unable to accept more work.");
