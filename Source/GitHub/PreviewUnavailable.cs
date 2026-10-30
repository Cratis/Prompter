// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// The provisional preview expired or was evicted before delivery completed.
/// </summary>
public sealed class PreviewUnavailable() : Exception("The preview is no longer available. Please draft it again.");
