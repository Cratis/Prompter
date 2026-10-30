// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>An incremental run would mix incompatible embedding spaces.</summary>
public class IndexProfileMismatch() : Exception("Embedding profile changed; use an approved compatible staging/full rebuild before deploying the new query model.");
