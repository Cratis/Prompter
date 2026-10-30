// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Another process owns the index lease. Callers must not queue indefinitely.
/// </summary>
public class IndexAlreadyRunning() : Exception("An index run is already active.");
